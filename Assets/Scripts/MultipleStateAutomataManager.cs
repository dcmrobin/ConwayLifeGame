using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.UIElements;

public enum CellState { Black_0, White_1, Red_2, Green_3, Blue_4 } // Add more states as needed

[System.Serializable]
public struct CustomRule {
    public int[] NeighborStatesToTriggerRule;
    [Range(0, 8)]
    public int[] NeighborCountsToTriggerRule; // Array of neighbor counts that trigger this rule
    public CellState OriginalState;
    public CellState TargetState; // State to change to
}

public class MultipleStateAutomataManager : MonoBehaviour
{
    int[,] cells;
    public List<CustomRule> customRules = new List<CustomRule>();

    [Header("UI")]
    public Slider densitySlider;
    public Slider delaySlider;
    public TMP_Dropdown cellToDrawDropdown;

    [Header("Controls")]
    [Range(0, 1)]
    public float density;
    public int width = 50;
    public int height = 50;
    public bool paused;
    public float updateDelay = 3;
    float delay;
    Texture2D texture;
    GameObject plane;
    RaycastHit hit;

    public void Start() {
        densitySlider.value = density;
        delaySlider.value = delay = updateDelay;
        cells = new int[width, height];
        texture = new(width, height);
        texture.filterMode = FilterMode.Point;

        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.Rotate(-90, 0, 0);
        plane.GetComponent<MeshRenderer>().material.mainTexture = texture;

        string[] cellStateContents = Enum.GetNames(typeof(CellState));
        List<string> contents = new List<string>(cellStateContents);
        cellToDrawDropdown.AddOptions(contents);
        cellToDrawDropdown.value = 1;

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
                colors[x + y * width] = (cells[x, y] == 1) ? Color.white : (cells[x, y] == 2 ? Color.red : (cells[x, y] == 3 ? Color.green : (cells[x, y] == 4 ? Color.blue : Color.black)));
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
                UpdateCustom();
                delay = delaySlider.value;
            }
        }

        HandleControls();
    }

    public void UpdateCustom()
    {
        int[,] newCells = new int[width, height];

        // Iterate over all cells
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                //int aliveNeighbors = GetSurroundingAliveCellCount(x, y);

                // Check custom rules
                foreach (CustomRule rule in customRules) {
                    for (int i = 0; i < rule.NeighborCountsToTriggerRule.Length; i++)
                    {
                        for (int j = 0; j < rule.NeighborStatesToTriggerRule.Length; j++)
                        {
                            if (cells[x, y] == (int)rule.OriginalState && rule.NeighborCountsToTriggerRule[i] == GetSurroundingCellOfStateCount(x, y, rule.NeighborStatesToTriggerRule[j])) {
                                newCells[x, y] = (int)rule.TargetState;
                                break;
                            }
                        }
                    }
                }
            }
        }

        // Update the cells array with the new state
        cells = newCells;

        // Render the updated grid
        Render();
    }

    int GetSurroundingCellOfStateCount(int gridX, int gridY, int cellState)
    {
        int aliveCellCount = 0;
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                int neighbourX = (gridX + offsetX + width) % width;
                int neighbourY = (gridY + offsetY + height) % height;
                if (cells[neighbourX, neighbourY] == cellState)
                {
                    aliveCellCount += cells[neighbourX, neighbourY];
                }
            }
        }
        // Subtract the central cell's value because it was added in the loop
        aliveCellCount -= cells[gridX, gridY];
        return aliveCellCount;
    }

    public void HandleControls()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            paused = !paused;

        if (Input.GetMouseButton(0))
        {
            SetCell(cellToDrawDropdown.value);
        }
        else if (Input.GetMouseButton(1))
        {
            SetCell(0);
        }
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
