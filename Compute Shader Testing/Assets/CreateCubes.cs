using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateCubes : MonoBehaviour
{

    [SerializeField] int size = 50;
    [SerializeField] float colorSpeed = 0.01f;
    [SerializeField] int repetitions = 1;
    [SerializeField] ComputeShader computeShader;
    [SerializeField] bool useGPU = false;

    // structures used in CPU version
    Vector3 startPos = new Vector3(0f, 0f, 0f);
    List<List<GameObject>> cubes = new List<List<GameObject>>();

    // variable to store the cube used for instantiation
    GameObject baseCube;

    // variabes used for GPU version
    public struct Cube {
        public Vector3 position;
        public Color color;
    };

    Cube[] data;    // this will keep track of the cubes for the GPU. 1D instead of 2D

    // Start is called before the first frame update
    void Start()
    {
        baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        data = new Cube[size * size];
        GenerateCubes();
    }

    // Update is called once per frame
    void Update()
    {
        // CPU version
        if (!useGPU) {
            for (int x = 0; x < repetitions; x++) {
                for (int i = 0; i < size; i++) {
                    for (int j = 0; j < size; j++) {
                        IncrementColor(i, j);
                        IncrementPosition(i, j);
                    }
                }
            }
        }

        // GPU version
        else {
            // create buffer from data (stores cube info for GPU) and set with data
            int colorSize = sizeof(float) * 4;
            int vector3Size = sizeof(float) * 3;
            int totalSize = colorSize + vector3Size;
            ComputeBuffer cubesBuffer = new ComputeBuffer(data.Length, totalSize);
            cubesBuffer.SetData(data);

            // set parameters in the Compute Shader
            computeShader.SetBuffer(0, "cubes", cubesBuffer);
            computeShader.SetFloat("resolution", data.Length);
            computeShader.SetFloat("repetitions", repetitions);
            computeShader.SetFloat("colorSpeed", colorSpeed);

            // dispatch the shader to run its main function
            computeShader.Dispatch(0, data.Length / 10, 1, 1);
            
            // get current data in cubes buffer after the shader function has run
            cubesBuffer.GetData(data);

            // update gameobject info after we've run the shader function
            for (int i = 0; i < size; i++) {
                for (int j = 0; j < size; j++) {
                    GameObject obj = cubes[i][j];
                    Cube cube = data[i * size + j];
                    obj.transform.position = cube.position;
                    obj.GetComponent<Renderer>().material.SetColor("_BaseColor", cube.color);
                }
            }
            cubesBuffer.Dispose();
        }
    }

    // creates the wall of cubes, call only once
    void GenerateCubes() {
        for (int i = 0; i < size; i++) {
            cubes.Add(new List<GameObject>());

            for (int j = 0; j < size; j++) {

                // generate the cubes position and instantiate it
                Vector3 newPos = startPos + new Vector3((float)i, (float)j, Random.Range(-0.2f, 0.2f));
                GameObject tempCube = Instantiate(baseCube, newPos, Quaternion.identity);

                // generate the cube's color and set it
                var cubeRenderer = tempCube.GetComponent<Renderer>();
                Color newColor = new Color(Random.Range(0.15f, 0.85f), Random.Range(0.15f, 0.85f), Random.Range(0.15f, 0.85f), 1f);
                cubeRenderer.material.shader = Shader.Find("Universal Render Pipeline/Lit");
                cubeRenderer.material.SetColor("_BaseColor", newColor);

                // add the cube to the gameobject list
                cubes[i].Add(tempCube);

                // update the data struct (used by GPU)
                Cube cubeData = new Cube();
                cubeData.position = newPos;
                cubeData.color = newColor;
                data[i * size + j] = cubeData;
            }
        }
    }

    // cycles the color through a preset range for a given cube (coordinates are provided in parameters)
    void IncrementColor(int i, int j) {

        // get cube and its color
        GameObject cube = cubes[i][j];
        var cubeRenderer = cube.GetComponent<Renderer>();
        Color color = cubeRenderer.material.GetColor("_BaseColor");

        // define the new color
        float newR = color.r + colorSpeed > 0.85f? 0.15f : color.r + colorSpeed;
        float newG = color.g + colorSpeed > 0.85f? 0.15f : color.g + colorSpeed;
        float newB = color.b + colorSpeed > 0.85f? 0.15f : color.b + colorSpeed;
        color = new Color(newR, newG, newB, 1f);

        // set the new color on the game object
        cubeRenderer.material.SetColor("_BaseColor", color);

        // update the data (for the GPU)
        data[i * size + j].color = color;
    }

    // generates a random position for a given cube //TODO change name from increment later
    void IncrementPosition(int i, int j) {
        GameObject cube = cubes[i][j];
        float currentZ = cube.transform.position.z;
        float newZ = Random.Range(-0.2f, 0.2f);
        cube.transform.position = new Vector3(cube.transform.position.x, cube.transform.position.y, newZ);
    }
}
