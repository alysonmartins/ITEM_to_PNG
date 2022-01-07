using System.IO;
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Threading.Tasks;


namespace ImageRenderer.Editor
{
    public class ImageRendererWindow : EditorWindow
    {
        private const string SAVE_PATH_PREF_KEY = "ImageRendererWindow.SavePath";
        private const int WINDOW_HORIZONTAL_BUFFER = 12;
        private const int WINDOW_VERTICAL_BUFFER = 115;
        private const int MAX_PREVIEW_DIMENSION = 500;
        private const float TEXTURE_HORIZONTAL_OFFSET_SPLIT = 0.5f;
        private const float TEXTURE_VERTICAL_OFFSET_SPLIT = 0.7f;
        private ImageFormat imageFormat = ImageFormat.png;


        private const int WAITING_TIME = 100;
        private const string DEFAULT_ITEMFOLDER = "Assets/_TOS/Resources/Items/Weapons"; // Prefab Root Folder
        Vector3 cameraPositionXYZ = new Vector3(2500, 0.48f, 2500); // Camera position in WORLD
        Vector3 itemHolderPositionXYZ = new Vector3(2500, 0.48f, 2502.07f); // Item Holder Position in WORLD
        private int textureWidth = 1200;
        private int textureHeight = 2048;

        private Camera renderCamera;
        GameObject itemHolder;

        private RenderTexture renderTexture;

        private string savePath = string.Empty;

        public Color color1 = Color.red;
        public Color color2 = Color.blue;


        public string componentName;
        private string imageName;

     
        private float ImageAspectRatio
        {
            get => (float)textureWidth / textureHeight;
        }

        #region Unity Methods

        private void OnGUI()
        {
            DrawImageSettings();
            DrawImagePreview();
            DrawSaveButtons();
        }
        private void OnDestroy()
        {
            DestroyImmediate(renderCamera.gameObject);
            DestroyImmediate(itemHolder.gameObject);
        }

        #endregion

        [MenuItem("TOS - Tools/Items to PNG &m")]


        private static void OpenWindow()
        {
            ImageRendererWindow window = GetWindow<ImageRendererWindow>(true, "[TOS] Item PNG Generator", true);
            Vector2 windowSize = new Vector2(MAX_PREVIEW_DIMENSION + WINDOW_HORIZONTAL_BUFFER, MAX_PREVIEW_DIMENSION + WINDOW_VERTICAL_BUFFER);
            window.minSize = windowSize;
            window.maxSize = windowSize;
            window.Initialize();
            window.Show();
        }


        private void Initialize()
        {
            if (renderCamera == null)
            {
                GameObject cameraObject = new GameObject("[TOS] Camera", typeof(Camera));
                renderCamera = cameraObject.GetComponent<Camera>();
                renderCamera.clearFlags = CameraClearFlags.SolidColor;
                renderCamera.transform.position = cameraPositionXYZ;
                renderCamera.backgroundColor = Color.magenta;

                
                itemHolder = new GameObject("[TOS] Item Holder");
                itemHolder.transform.position = itemHolderPositionXYZ;
            }

            Selection.activeGameObject = renderCamera.gameObject;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.magenta;


            savePath = EditorPrefs.GetString(SAVE_PATH_PREF_KEY, String.Empty);

            RebuildRenderTarget();
        }


        private void RebuildRenderTarget()
        {
            if (renderTexture != null)
            {
                renderCamera.targetTexture = null;
                DestroyImmediate(renderTexture);
            }

            renderTexture = new RenderTexture(textureWidth, textureHeight, 32);
            renderCamera.targetTexture = renderTexture;
            renderCamera.aspect = ImageAspectRatio;

            

            // Clears the active render target (our texture in this case)
            GL.Clear(true, true, Color.clear);
        }


        private void DrawImageSettings()
        {

            GUILayout.Label("Image Settings", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            { 
                // select
                imageFormat = (ImageFormat)EditorGUILayout.EnumPopup("Image Format", imageFormat);
                // select
            }
        }

        private void DrawImagePreview()
        {
            GUILayout.Label("Image Preview", EditorStyles.boldLabel);

            int maxPreviewDimension = Mathf.Min(Mathf.Max(textureWidth, textureHeight), MAX_PREVIEW_DIMENSION);
            float previewWidth = ImageAspectRatio >= 1f ? maxPreviewDimension : maxPreviewDimension * ImageAspectRatio;
            float previewHeight = ImageAspectRatio >= 1f ? maxPreviewDimension / ImageAspectRatio : maxPreviewDimension;

            Rect previewTextureRect = new Rect(
                WINDOW_HORIZONTAL_BUFFER * TEXTURE_HORIZONTAL_OFFSET_SPLIT + (MAX_PREVIEW_DIMENSION - previewWidth) / 2,
                WINDOW_VERTICAL_BUFFER * TEXTURE_VERTICAL_OFFSET_SPLIT / 1.3f, previewWidth, previewHeight);

            EditorGUI.DrawPreviewTexture(previewTextureRect, renderTexture);
            

        }


        private void DrawSaveButtons()
        {
            const float SAVE_FIELDS_OFFSET = MAX_PREVIEW_DIMENSION + 20;
            const float BUTTON_WIDTH = 100f;

            bool selectPathClicked = false;
            bool saveClicked = false;
            bool genNewItemClicked = false;

            bool prevItemClicked = false;
            bool nextItemClicked = false;


            GUILayout.Space(SAVE_FIELDS_OFFSET);
            using (new EditorGUILayout.HorizontalScope())
            {
                savePath = EditorGUILayout.TextField(savePath, GUILayout.ExpandWidth(true));
            }
            using (new EditorGUILayout.HorizontalScope())
            { 
                
                selectPathClicked = GUILayout.Button("(disabled)", GUILayout.Width(BUTTON_WIDTH));


                prevItemClicked = GUILayout.Button("Prev. Item (d)", GUILayout.Width(BUTTON_WIDTH));
                nextItemClicked = GUILayout.Button("Next Item (d)", GUILayout.Width(BUTTON_WIDTH));

                saveClicked = GUILayout.Button("Save", GUILayout.Width(BUTTON_WIDTH));
                genNewItemClicked = GUILayout.Button("Save All Items", GUILayout.Width(BUTTON_WIDTH));

            }

            if (selectPathClicked)
            {
                //savePath = EditorUtility.SaveFilePanel("Save Location", Application.dataPath, DEFAULT_FILENAME, imageFormat.ToString());
            }

            if (saveClicked)
            {
                Texture2D convertedTexture = ConvertToTexture2D(renderTexture);
                SaveTexture(convertedTexture);
                EditorPrefs.SetString(SAVE_PATH_PREF_KEY, savePath);
            }

            if (genNewItemClicked) // Function SaveAllItems 
            {
                LoadItemOnHolder(); // Call Action
            }

            // New Feature this 'll be avaiable in the next updates

            if (prevItemClicked) // botão gerar all images 
            {
               //TODO: Function to Call Previous Item.
            }
            if (genNewItemClicked) // botão gerar all images 
            {
                //TODO: Function to Call Next Item.
            }

        }


        



        private async void LoadItemOnHolder()
        {

            var creaturesPrefabs = AssetDatabase.FindAssets("t:prefab", new string[] { DEFAULT_ITEMFOLDER })
            .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
            .OrderBy(go => go.name).ToArray(); // Load all .prefab inside the DEFAULT_ITEMFOLDER and put on a ARRAY
            
            
            foreach (GameObject go in creaturesPrefabs)
            {


                GameObject a = Instantiate(go) as GameObject; // Call Prefab to GameObject

                try
                {


                //Setando posição do item no mundo
                a.transform.position = itemHolderPositionXYZ;

               
                a.transform.parent = itemHolder.transform; // Put the Item / GameObject in ItemHolder

                var hinge = a.GetComponentInChildren<Renderer>().bounds.size; // Get the bound Extends from children 

                    
                Debug.Log(go.name + " -> " + hinge.y); // Print the information for analitycs

                Vector3 newPos = new Vector3 (0, 0 - (hinge.y / 2), 0);


                    Debug.Log(newPos);

                a.transform.Translate(newPos);

                //renderCamera.transform.LookAt(newPos);



                }
                catch { 


                }
                













                // // Make the camera Look Directly to Item (Item go to center)

                float screenRatio = (float)Screen.width / (float)Screen.height;

                imageName = go.name; // Get name from .prefab file to use later to save file

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); // Refresh all Unity GUI (need to regen the Camera Preview)
  
                await Task.Delay(WAITING_TIME); // Delay to wait all system refresh.. to continue

                Texture2D convertedTexture = ConvertToTexture2D(renderTexture); // Converte Camera Preview to a Texture

                SaveTexture(convertedTexture); // Call function to save the Texture to PNG file

                DestroyImmediate(a); // Destroy GameObject

            }


        }


        private Texture2D ConvertToTexture2D(RenderTexture renderTexture)
        {
            RenderTexture previousRenderTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Texture2D savableTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            savableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            savableTexture.Apply();

            RenderTexture.active = previousRenderTexture;

            return savableTexture;

        }



        public void SaveTexture(Texture2D texture)
        {
            byte[] encodedImageData;

            switch (imageFormat)
            {
                case ImageFormat.jpg:
                    encodedImageData = texture.EncodeToJPG();
                    break;
                case ImageFormat.png:
                    encodedImageData = texture.EncodeToPNG();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            savePath = $"{Application.dataPath}/{imageName}.{imageFormat}";
            File.WriteAllBytes(savePath, encodedImageData);
            
        }


        private enum ImageFormat
        {
            jpg,
            png
        }


    }


}
