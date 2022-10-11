
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
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
    bool undo_canceled;
    public TMP_Dropdown dropdown;
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
        redo_pixel_stack.Clear();
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
        previousPosition.x = -100f;
        previousPosition.y = -100f;

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
        if (!dropdown.IsExpanded && !undoing && !redoing)
        {

            Vector3 pos = Input.mousePosition;
            pos.y -= 25.725f;
            /*   if(undoing && Input.GetMouseButtonUp(0))
               {
                   undoing = false;
               }
               if(redoing && Input.GetMouseButtonUp(0))
               {
                   redoing = false;
               }*/
            if (Input.GetMouseButton(0))
            {


                if (pos.y > 50 &&(pos.x<868||pos.x>910))
                {
                    //pos.y -=transparent.transform.position.y;
                    undo_pixel_stack.Push(new Pixel(pos, clr, brushSize));
                    was_mouse_down = true;
                    brushDraw(pos, brushSize, clr);
                    brushLine(previousPosition, pos, brushSize, clr);
                    previousPosition = pos;
                    if(undo_canceled)
                    {
                        undo_canceled = false;
                        redo_pixel_stack.Clear();
                    }
                }
                else if (was_mouse_down)
                    {
                        undo_pixel_stack.Push(new Pixel(new Vector2(-100, -100), clr, brushSize));
                    }
            }
            else
            {
                if (was_mouse_down)
                {
                    undo_pixel_stack.Push(new Pixel(new Vector2(-100, -100), clr, brushSize));
                    was_mouse_down = false;
                }
                if (pos.y < 50)
                {
                    previousPosition.x = -100;
                    previousPosition.y = -100;
                }
                else
                    previousPosition = pos;
            }

            // drawStack();

            drawing_tex.SetPixels32(color_array);
            drawing_tex.Apply();       //writing these here so that they may be callled only once per frame
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
        int last_index = undo_pixel_stack.Count - 1;
        Pixel previous =undo_pixel_stack.ElementAt(last_index);
        Pixel present;
        for (int i = last_index-1; i >0; i--)
        {
            present = undo_pixel_stack.ElementAt(i);
            brushLine(present.position, previous.position, present.pixelSize, present.color);
            previous = present;
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

    public void Color_Changer()
    {
        
        if (eraseMode)
        {
            eraseMode = false;
            clr = temporary_color;
        }
        var value = dropdown.value;
        switch (value)
        {
            case 0:
                clr =Color.black;
                break;
            case 1:
                clr = Color.red;
                break;
            case 2:
                clr = Color.green;
                break;
            case 3:
                clr = Color.blue;
                break;

            default:
                clr = Color.cyan;
                break;
        }

    }
    public void onSave()
    {
        System.Random rnd = new System.Random();

        String filepath = Application.persistentDataPath + "whiteboardimg" + rnd.Next() + ".png";
        Debug.Log(filepath);
        Byte[] filebytes = drawing_tex.EncodeToPNG();
        //FileStream file = File.OpenWrite(filepath);
        File.WriteAllBytes(filepath, filebytes);

    }

    public void undoDown()
    {
        if(eraseMode)
        {
        eraseMode = false;
        clr = temporary_color;
        }
        undoing = true;
        
        int count = undo_pixel_stack.Count;
        if (count > 0)
        {
            Pixel pop = undo_pixel_stack.Pop();
            count--;
            while (pop.position.x == -100 && count > 0)
            {
                pop = undo_pixel_stack.Pop();
                count--;
            }
            redo_pixel_stack.Push(pop);
                while (pop.position.x != -100 && count>0)
                {
                    pop = undo_pixel_stack.Pop();
                    redo_pixel_stack.Push(pop);
                    count--;
                }
            redo_pixel_stack.Push(pop);
            RenderClear();
            if(count>0)
            {
            drawStack();
            }
            //undo_pixel_stack.Push(new Pixel(last_element, clr, brushSize));
        }
    }
    public void undoUp()
    {
        
        undo_pixel_stack.Push(new Pixel(new Vector2(-100, -100), clr, brushSize));
        undoing = false;
        undo_canceled = true;

    }
    public void redoDown()
    {
        
        if(eraseMode)
        {
            eraseMode = false;
            clr = temporary_color;
        }
        redoing = true;
        int count = redo_pixel_stack.Count;
        if (count>0)
        {
            Pixel pop = redo_pixel_stack.Pop();
            count--;
            while (pop.position.x == -100 && count > 0)
            {
                pop = redo_pixel_stack.Pop();
                count--;
            }
            undo_pixel_stack.Push(pop);
            while (pop.position.x != -100 && count>0)
            {
                pop = redo_pixel_stack.Pop();
                count--;
                undo_pixel_stack.Push(pop);
            }
            undo_pixel_stack.Push(pop);
            RenderClear();
            drawStack();
            //undo_pixel_stack.Push(new Pixel(last_element, clr, brushSize));
        }
    }
    public void redoUp()
    {
        undo_pixel_stack.Push(new Pixel(new Vector2(-100, -100), clr, brushSize));
        redoing = false;
    }

}



