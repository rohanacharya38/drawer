
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

using UnityEngine;

using UnityEngine.UI;


public class Drawer : MonoBehaviour

{

    public float brushSize = 0.1f;
    public Color32[] color_array;         //color array representing each pixel of our texture
    public Color32 clr = Color.black;     //color of our brush
    public Color32[] clean_color_array;   //color array representing each pixel of our texture but empty used to clear screen
    public Color32 Default_Color;         //default color of the texture
    public Image transparent;
    public Texture2D drawing_tex;
    public Color temporary_color;
    bool was_mouse_down;
    bool undoing;
    bool redoing;
    public TMP_Dropdown dropdown;
    bool on_screen_collision;
    bool eraseMode = false;
    public Slider slider;
    public TextMeshProUGUI sliderText;
    Vector3 previousPosition;
    public struct Pixel
    {
        public Color32 color;
        public Vector2 position;
        public float pixelSize;
        public Pixel(Vector2 position, Color32 color, float pixelSize)
        {
            this.color = color;
            this.position = position;
            this.pixelSize = pixelSize;

        }
    };
    public Stack<Pixel> undo_pixel_stack;
    public Stack<Pixel> redo_pixel_stack;
    /*    public int BrushSize = 1;
        public Texture2D tex = null;*/
    public void Clear()
    {
        RenderClear();
        drawing_tex.Apply();
        undo_pixel_stack.Clear();
    }
    //TODO: ASSIGN COLOR SPRITES FOR COLOR DROPDOWN
    private void Start()
    {

        slider.onValueChanged.AddListener((v) =>
        {
            sliderText.text = v.ToString("0.01");
            if (v < 0.01)
            {
                brushSize = 0.01f;
            }
            else
                brushSize = v;

        });

        drawing_tex = transparent.sprite.texture;
        Default_Color = Color.yellow;
        clr = Color.black;
        // Initialize clean pixels to use
        color_array = new Color32[drawing_tex.width * drawing_tex.height];

        clean_color_array = new Color32[(int)transparent.sprite.rect.width * (int)transparent.sprite.rect.height];
        for (int x = 0; x < clean_color_array.Length; x++)
            clean_color_array[x] = Default_Color;
        // Initialize pixels to use
        for (int x = 0; x < color_array.Length; x++)
            color_array[x] = Default_Color;
        drawing_tex.SetPixels32(color_array);
        drawing_tex.Apply();
        undo_pixel_stack = new Stack<Pixel>();
        redo_pixel_stack = new Stack<Pixel>();

    }
    public void RenderClear()       //it is named as render clear as im familiar with SDL
    {
        for (int i = 0; i < color_array.Length; i++)
        {
            color_array[i] = Default_Color;
        }
    }
    private void Update()
    {
        if (!dropdown.IsExpanded)
        {

            Vector3 pos = Input.mousePosition;
            if (pos.y > 50)
                on_screen_collision = true;
            else
                on_screen_collision = false;

            //pos.y -=transparent.transform.position.y;
            pos.y -= 25.725f;

            if (Input.GetMouseButton(0) && on_screen_collision)
            {
                if (undoing)
                {
                    undo_pixel_stack.Push(new Pixel(new Vector2(-100, -100), clr, brushSize));
                    undoing = false;
                }
                if(redoing)
                {
                    undo_pixel_stack.Push(new Pixel(new Vector2(-100, -100), clr, brushSize));
                    redoing = false;
                }
                undo_pixel_stack.Push(new Pixel(pos, clr, brushSize));
                was_mouse_down = true;
                int count = undo_pixel_stack.Count;
                    brushDraw(pos, brushSize, clr);
                    brushLine(previousPosition, pos, brushSize, clr);
            }
            else if (Input.GetMouseButton(0))
            {

                if (undoing)
                {
                    RenderClear();
                    if (undo_pixel_stack.Count > 0)
                    {
                       redo_pixel_stack.Push( undo_pixel_stack.Pop());

                        //undo_pixel_stack.Push(new Pixel(last_element, clr, brushSize));
                    }
                    drawStack();
                }
                if (redoing)
                {
                    RenderClear();
                    if (redo_pixel_stack.Count > 0)
                    {
                        undo_pixel_stack.Push(redo_pixel_stack.Pop());
                    }
                    drawStack();
                }
                else if (was_mouse_down)
                {
                    undo_pixel_stack.Push(new Pixel(new Vector2(-100, -100), clr, brushSize));
                }
                was_mouse_down = false;

            }
            else
            {
                if (was_mouse_down)
                {

                    undo_pixel_stack.Push(new Pixel(new Vector2(-100, -100), clr, brushSize));


                }
                was_mouse_down = false;
            }

            // drawStack();

            drawing_tex.SetPixels32(color_array);
            drawing_tex.Apply();            //writing these here so that they may be callled only once per frame
            previousPosition = pos;
        }
    }
    private void brushDraw(Vector2 pixelUV, float pixelSize, Color curColor) //implemented draw method without the need of a stack
    {

        for (int i = (int)(pixelUV.x - pixelSize * 100); i < (int)(pixelUV.x + pixelSize * 100); i++)
        {
            for (int j = (int)(pixelUV.y - pixelSize * 100); j < (int)(pixelUV.y + pixelSize * 100); j++)
            {

                int pixelIndex = j * drawing_tex.width + i;
                if (pixelIndex < 0 || pixelIndex >= color_array.Length)
                    return;
                color_array[pixelIndex] = curColor;
            }
        }
    }
    public void brushLine(Vector2 start, Vector2 end, float pixelSize, Color curColor)
    {
        if (start.x == -100 && start.y == -100)
        {
            return;
        }
        if (end.x == -100 && end.y == -100)
            return;
        float distance = Vector2.Distance(start, end);
        Vector2 direction = (end - start).normalized;
        for (int i = 0; i < distance; i++)
        {
            brushDraw(start + direction * i, pixelSize, curColor);
        }
    }


    public void drawStack()
    {
        for (int i = 1; i < undo_pixel_stack.Count; i++)
        {
            brushLine(undo_pixel_stack.ElementAt(i).position, undo_pixel_stack.ElementAt(i - 1).position, undo_pixel_stack.ElementAt(i).pixelSize, undo_pixel_stack.ElementAt(i).color);
        }

    }
    public void Undo()
    {
        undoing = true;
        redoing = false;
    }
    public void Redo()
    {
        if(undoing)
        {
            undoing = false;
            redoing = true;
        }
    }
    public void Eraser()
    {
        if (!eraseMode)
        {
            eraseMode = true;
            temporary_color = clr;
            clr = Default_Color;

        }
        else
        {
            clr = temporary_color;
            eraseMode = false;

        }

    }

    public void Color_Changer(Int32 value)
    {
        if (eraseMode)
        {
            eraseMode = false;
            clr = temporary_color;
        }
        value = dropdown.value;



        switch (value)
        {
            case 0:
                clr = Color.black;
                break;
            case 1:
                clr = Color.red;
                break;
            case 2:
                clr = Color.blue;
                break;
            case 3:
                clr = Color.green;
                break;

            default:
                clr = Color.cyan;
                break;
        }

    }

}



