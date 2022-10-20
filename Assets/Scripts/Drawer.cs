
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Drawer : MonoBehaviour

{
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
        public static Pixel myZero = new Pixel(new Vector2(-100, -100), Color.cyan, 0.00f);
    };

    //UI Objects //assigned by drag and drop
    public Image transparent;
    public TMP_Dropdown dropdown;
    public Slider slider;
    public TextMeshProUGUI sliderText;
    public RectTransform bottom_rect;
    public Sprite eraserRed;
    public Sprite eraserBlack;
    public Button eraseButton;
    public Button dayNight;

    //variables changed by ui elements or depending on UI elements
    Color32 clr = Color.black;     //color of our brush
    float brushSize = 0.1f;         //size of brush
    Color32 Default_Color;         //default color of the texture
    RectTransform canvasRect;              //Rect Transform of background Image


    //Boolean variable representing different situations
    bool was_mouse_down;
    bool undoing;
    bool redoing;
    bool undo_canceled;
    bool eraseMode = false;
    bool sliderDragging;

    //app data
    Vector3 previousPosition;
    Texture2D drawing_tex;          //A reference to backround image's texture is stored here which is modified in game
    Color32[] color_array;         //color array representing each pixel of our texture
    List<Pixel> undo_pixel_list;    //this list stores all the pixel in image that the user pressed
    List<Pixel> redo_pixel_list;    //while undoing, undoed pixels are pushed here
   
    //TODO: ASSIGN COLOR SPRITES FOR COLOR DROPDOWN


    //start is called first after opening game
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

        });     //callback function when the value on slider is changed

        //Color setup
        drawing_tex = transparent.sprite.texture;       //assigning bg image reference to drawing_tex
        Default_Color = Color.yellow;                   //default color of bg
        clr = Color.black;                              //default drawing color
        color_array = new Color32[(int)drawing_tex.width * (int)drawing_tex.height];        //the array representing each pixel of screen
        // Initialize pixels to use
        for (int x = 0; x < color_array.Length; x++)
            color_array[x] = Default_Color;
        drawing_tex.SetPixels32(color_array);
        drawing_tex.Apply();


        //Initializing list for use
        undo_pixel_list= new List<Pixel>();
        redo_pixel_list= new List<Pixel>();


        //In my draw function, it won't draw a pixel in -100 position, so it means dont draw
        previousPosition = new Vector3(-100, -100);
        //Setting screen orientation for androids
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        canvasRect= new RectTransform();
        canvasRect = (RectTransform)transform;

    }
    //Clears every pixel to the default color i.e. bg color
    public void RenderClear()       //it is named as render clear as im familiar with SDL
    {
        for (int i = 0; i < color_array.Length; i++)
        {
            color_array[i] = Default_Color;
        }
    }

    //since the canvas width and height vary according to device,this function gives the exact position of mouse inside the image
    Vector2 mouseToImagePixels(Vector3 mouseScreenPosition)
    {
        Vector2 pointInImage;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreenPosition, null, out pointInImage);        //this function takes the position of mouse in screen and returns its local position in out variable
        //Converting the local position in rect to pixel position in image
        //point to note here is we are converting to Image Pixels
        pointInImage -= canvasRect.rect.min; 
        //origin to 0,0
        pointInImage.x *= transparent.sprite.rect.width / canvasRect.rect.width;
        //if image>canvas this will scale up the point otherwise scale down
        pointInImage.y *= transparent.sprite.rect.height / canvasRect.rect.height;
        //hence new origin is the origin of image
        pointInImage += transparent.sprite.rect.min;
        //origin to origin of image
        
        return pointInImage;
    }
    private void Update()
    {

        if (!dropdown.IsExpanded && !undoing && !redoing && !sliderDragging)    //we shouldn;t draw in any of these 4 states
        {
            if (Input.GetMouseButton(0))                //returns true when mouse/touch is held down/initiated
            {

                Vector2 pos = Input.mousePosition;          //pos contains position of mouse in Screen

                if (!RectTransformUtility.RectangleContainsScreenPoint(bottom_rect,pos) && !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)dayNight.transform, pos))           //the bottom rect is the rect around the buttons on bottom of screen
                {
                    pos = mouseToImagePixels(pos);              
                    undo_pixel_list.Add(new Pixel(pos, clr, brushSize));    //pushing the pixel to our undo list
                    was_mouse_down = true;      //if the mouse is lifted in the next frame then this boolean is used for linebreak
                    brushLine(previousPosition, pos, brushSize, clr);       //draws line from previous position to current position
                    previousPosition = pos;
                    if (undo_canceled)                          //if the user was undoing and he presses on any point on the screen,no more redo
                    {
                        undo_canceled = false;
                        redo_pixel_list.Clear();
                    }
                }
            }
            else
            {
                if (was_mouse_down)// if mouse was down in previous frame but not in this
                {
                    undo_pixel_list.Add(Pixel.myZero);
                    was_mouse_down = false;
                    previousPosition.x = -100;
                    previousPosition.y = -100;
                }

            }
        }
        // drawStack();
        drawing_tex.SetPixels32(color_array);       //the pixels of drawing_tex is updated with values in color array
        drawing_tex.Apply();      
        //writing these here so that they may be callled only once per frame



    }
    private void brushDraw(Vector2 pixelUV, float pixelSize, Color curColor) //draws a point to image //kinda like a putpixel function
    {
        //here pixelSize is the current brushSize and color is the current color of the brush being used
        if (pixelUV.x == -100)
            return; //dont draw call
        if(curColor==Color.white ||curColor==Color.yellow)
        {
            curColor = Default_Color;
        }
        //Colors the block of 100*pixelSize by 100* pixelSize with the given color
        for (int i = (int)(pixelUV.x - pixelSize * 100); i < (int)(pixelUV.x + pixelSize * 100); i++)
        {
            for (int j = (int)(pixelUV.y - pixelSize * 100); j < (int)(pixelUV.y + pixelSize * 100); j++)
            {

                int pixelIndex = j * (int)drawing_tex.width + i;            //2d array to 1d array conversion formula
                if (pixelIndex < 0 || pixelIndex >= color_array.Length) 
                    return;
                
                color_array[pixelIndex] = curColor;         
            }
        }
    }
    public void brushLine(Vector2 start, Vector2 end, float pixelSize, Color curColor)
    {
        //drawing line from start to end
        if (start.x == -100 && start.y == -100)
        {
            brushDraw(end, pixelSize, curColor);
            return;  
        }
        //if start is dont draw then try drawing end point only and vice versa
        if (end.x == -100 && end.y == -100)
        {
            brushDraw(start, pixelSize, curColor);
            return;
        }

        float distance = Vector2.Distance(start, end); //magnitude of distance between start and end
        Vector2 direction = (end - start).normalized; // unit vector representing start to end
        for (int i = 0; i < distance; i++)
        {
            brushDraw(start + direction * i, pixelSize, curColor);

            //drawing each point from start to end 1 units apart
            // direction *i gives the vector i the distance from start to end
        }
    }

    //draws the undo list on screen
    public void drawList()
    {
        if (undo_pixel_list.Count > 0)
        {
            Pixel current;
            Pixel previous = undo_pixel_list[0];

            for (int i = 1; i < undo_pixel_list.Count; i++)
            {
                current = undo_pixel_list[i];
                if (current.position.x == -100)
                {
                    brushDraw(previous.position, previous.pixelSize, previous.color);
                }
                else
                    brushLine(current.position, previous.position, previous.pixelSize, previous.color);

                previous = current;
            }
        }
        
    }

    //changes the current color to default color to imitate the action of a eraser

    public void Eraser()
    {
        if (!eraseMode) //eraser is not active and user presses eraser
        {
            eraseMode = true;
            clr = Default_Color;
            eraseButton.image.sprite = eraserRed;
        }
        else //eraser is active and user presses eraser
        {
            Color_Changer();    //JUST ME BEING LAZY
        }

    }
    //The button event functions




    //color change function
    public void Color_Changer()
    {

        if (eraseMode)
        {
            eraseMode = false;
            eraseButton.image.sprite = eraserBlack;
        }
        var value = dropdown.value;
        switch (value)
        {
            case 0:
                clr = Color.black;
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
        File.WriteAllBytes(filepath, filebytes);
    }
    //on one press erases the line between 2 latest dont draws
    public void undoDown()
    {
        undoing = true;
        int count = undo_pixel_list.Count-1;
        if (count >=0)
        {
            Pixel pop = undo_pixel_list[count];
            undo_pixel_list[count] = Pixel.myZero;
            count--;
            while (pop.position.x == -100 && count >=0)
            {
                pop = undo_pixel_list[count];
                undo_pixel_list[count] = Pixel.myZero;
                count--;
            }
            redo_pixel_list.Add(pop);
            if(count>=0)
            {
            while (pop.position.x != -100 && count >= 0)
            {
                pop = undo_pixel_list[count];
                undo_pixel_list[count] = Pixel.myZero;
                redo_pixel_list.Add(pop);
                count--;
            }
                redo_pixel_list.Add(pop);
            }
            RenderClear();
            if (count >=0) 
            {
                drawList();
            }
        }
    }


    public void undoUp()
    {
        undo_pixel_list.Add(Pixel.myZero);
        undoing = false;
        undo_canceled = true;
    }
    public void redoDown()
    {
        redoing = true;
        int count = redo_pixel_list.Count-1;
        if (count >= 0)
        {
            Pixel pop = redo_pixel_list[count];
            redo_pixel_list[count] = Pixel.myZero;
            count--;
            while (pop.position.x == -100 && count >= 0)
            {
                 pop = redo_pixel_list[count];
                redo_pixel_list[count] = Pixel.myZero;
                count--;
            }
            undo_pixel_list.Add(pop);
            while (pop.position.x != -100 && count >=0)
            {
                pop = redo_pixel_list[count];
                redo_pixel_list[count] = Pixel.myZero;
                count--;
                undo_pixel_list.Add(pop);
            }
            undo_pixel_list.Add(pop);
            RenderClear();
            drawList();
        }
    }
    public void redoUp()
    {
        undo_pixel_list.Add(Pixel.myZero);
        redoing = false;
    }
    public void Clear()
    {

        RenderClear();
        drawing_tex.Apply();
        undo_pixel_list.Clear();
        redo_pixel_list.Clear();
    }
    public void sliderBeginDrag() { sliderDragging = true; }
    public void sliderEndDrag(){sliderDragging=false;}

    public void bgChange()
    {
        if (Default_Color == Color.yellow)
        {
            dayNight.image.color=Color.yellow;
            Default_Color = Color.white;
            RenderClear();
            drawList();
        }
        else
        {
            dayNight.image.color = Color.white;
            Default_Color = Color.yellow;
            RenderClear();
            drawList();
        }
    }

}



