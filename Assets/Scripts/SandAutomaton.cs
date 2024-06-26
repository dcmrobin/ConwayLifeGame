using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using TMPro;
using Unity.VisualScripting;

public class SandAutomaton : MonoBehaviour
{
    [HideInInspector] public int[,] cells;

    [Header("UI")]
    public Slider densitySlider;
    public Slider delaySlider;
    public TMP_Dropdown cellTypeDropdown;

    [Header("Controls")]
    [Range(0, 1)]
    public float density;
    public int width = 50;
    public int height = 50;
    public bool paused;
    public float updateDelay = 3;
    float delay;
    [HideInInspector] public Texture2D texture;
    GameObject plane;
    [HideInInspector] public RaycastHit hit;

    public void Start() {
        if (GameObject.Find("Menu") != null && GameObject.Find("Menu").GetComponent<Loader>().sizeInputfield.text != "")
        {
            width = Convert.ToInt32(GameObject.Find("Menu").GetComponent<Loader>().sizeInputfield.text);
            height = Convert.ToInt32(GameObject.Find("Menu").GetComponent<Loader>().sizeInputfield.text);
        }
        else
        {
            width = 100;
            height = 100;
        }

        densitySlider.value = density;
        delaySlider.value = delay = updateDelay;
        cells = new int[width, height];
        texture = new(width, height);
        texture.filterMode = FilterMode.Point;

        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.Rotate(-90, 0, 0);
        plane.GetComponent<MeshRenderer>().material.mainTexture = texture;
        plane.GetComponent<MeshRenderer>().material.SetFloat("_Glossiness", 0);

        GenerateRandomCells();
    }

    public void GenerateRandomCells()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = (UnityEngine.Random.value < densitySlider.value)?1:0;
            }
        }
        Render();
    }

    public void Clear()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = 0;
            }
        }
        Render();
    }

    public void Render()
    {
        Color[] colors = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                colors[x + y * width] = cells[x, y] == 1 ? Color.yellow : cells[x, y] == 2 ? Color.cyan : cells[x, y] == 3 ? Color.gray : Color.black;
            }
        }
        texture.SetPixels(colors);
        texture.Apply();
    }

    public void Update() {
        if (!paused)
        {
            delay -= .1f;
            if (delay <= 0)
            {
                UpdateCells();
                delay = delaySlider.value;
            }
        }

        HandleControls();
    }

    public void UpdateCells()
    {
        int[,] newCells = new int[width, height];

        // Iterate over all cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // If the current cell is empty, continue to the next cell
                if (cells[x, y] == 0)
                    continue;

                
                if (x < width - 1 && x > 0 && y < height - 1)
                {
                    if (cells[x, y] == 1)// if sand
                    {
                        if (cells[x, y + 1] == 0)// gravity
                        {
                            int maxFallDistance = 5;
                            int fallDistance = 0;

                            // Check up to 5 cells downwards
                            for (int i = 1; i <= maxFallDistance; i++)
                            {
                                // Check if within bounds
                                if (y + i >= height)
                                {
                                    fallDistance = i - 1;
                                    break;
                                }

                                // Check if the cell below is empty
                                if (cells[x, y + i] == 0)
                                {
                                    fallDistance = i;
                                }
                                else
                                {
                                    // Stop if a solid cell is encountered
                                    fallDistance = i - 1;
                                    break;
                                }
                            }

                            // Move the sand particle down by the determined fall distance
                            newCells[x, Mathf.Min(y + fallDistance, height - 1)] = 1;
                            continue;
                        }

                        if (cells[x - 1, y + 1] == 0 && cells[x + 1, y + 1] == 0)
                        {
                            if (cells[x, y + 1] != 2)
                            {
                                if (UnityEngine.Random.value > 0.5)
                                {
                                    newCells[x - 1, y + 1] = 1;
                                    continue;
                                }
                                if (UnityEngine.Random.value < 0.5)
                                {
                                    newCells[x + 1, y + 1] = 1;
                                    continue;
                                }
                            }
                        }
                        else if (cells[x - 1, y + 1] == 0)
                        {
                            if (cells[x, y + 1] != 2)
                            {
                                newCells[x - 1, y + 1] = 1;
                                continue;
                            }
                        }
                        else if (cells[x + 1, y + 1] == 0)
                        {
                            if (cells[x, y + 1] != 2)
                            {
                                newCells[x + 1, y + 1] = 1;
                                continue;
                            }
                        }
                    }
                    if (cells[x, y] == 2) // if water
                    {
                        // Gravity: If the cell below is empty, move down
                        if (y > 0 && cells[x, y + 1] == 0)
                        {
                            int maxFallDistance = 5;
                            int fallDistance = 0;

                            // Check up to 5 cells downwards
                            for (int i = 1; i <= maxFallDistance; i++)
                            {
                                // Check if within bounds
                                if (y + i >= height)
                                {
                                    fallDistance = i - 1;
                                    break;
                                }

                                // Check if the cell below is empty
                                if (cells[x, y + i] == 0)
                                {
                                    fallDistance = i;
                                }
                                else
                                {
                                    // Stop if a solid cell is encountered
                                    fallDistance = i - 1;
                                    break;
                                }
                            }

                            // Move the sand particle down by the determined fall distance
                            newCells[x, Mathf.Min(y + fallDistance, height - 1)] = 2;
                            continue;
                        }

                        // Check for horizontal movement
                        bool canMoveLeft = x > 0 && cells[x - 1, y] == 0;
                        bool canMoveRight = x < width - 1 && cells[x + 1, y] == 0;

                        // Move left if only left is empty
                        if (canMoveLeft && !canMoveRight)
                        {
                            if (newCells[x - 1, y] != 2) // Only move if target cell is not water
                            {
                                newCells[x - 1, y] = 2;
                                continue;
                            }
                        }
                        // Move right if only right is empty
                        else if (canMoveRight && !canMoveLeft)
                        {
                            if (newCells[x + 1, y] != 2) // Only move if target cell is not water
                            {
                                newCells[x + 1, y] = 2;
                                continue;
                            }
                        }
                        // Randomly choose between left and right if both are empty
                        else if (canMoveLeft && canMoveRight)
                        {
                            if (UnityEngine.Random.value > 0.5f)
                            {
                                if (newCells[x - 1, y] != 2) // Only move if target cell is not water
                                {
                                    newCells[x - 1, y] = 2;
                                    continue;
                                }
                            }
                            else if (UnityEngine.Random.value < 0.5f)
                            {
                                if (newCells[x + 1, y] != 2) // Only move if target cell is not water
                                {
                                    newCells[x + 1, y] = 2;
                                    continue;
                                }
                            }
                        }
                    }
                }

                // If none of the above conditions are met, the particle remains where it is
                newCells[x, y] = cells[x, y] == 1 ? 1 : cells[x, y] == 2 ? 2 : cells[x, y] == 3 ? 3 : 0;
            }
        }

        // Update the cells array with the new state
        cells = newCells;

        // Render the updated grid
        Render();
    }

    public void HandleControls()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            paused = !paused;

        if (Input.GetMouseButton(0))
        {
            SetCell(cellTypeDropdown.value + 1);
        }
        else if (Input.GetMouseButton(1))
        {
            SetCell(0);
        }
        //else if (Input.GetMouseButtonDown(2)) // Middle mouse button
        //{
        //    GetComponent<LifeShapesDatabase>().PlacePredefinedShape(GetComponent<LifeShapesDatabase>().predefinedShapes[GetComponent<LifeShapesDatabase>().predefinedShapesDropdown.value]);
        //}
    }

    public void SetCell(int cellValue)
    {
        if (Physics.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Camera.main.transform.forward, out hit, Mathf.Infinity))
        {
            Vector2 pixelUV = hit.textureCoord;
            pixelUV.x *= texture.width;
            pixelUV.y *= texture.height;
            cells[(int)pixelUV.x, (int)pixelUV.y] = cellValue;
            Render();
        }
    }
}
