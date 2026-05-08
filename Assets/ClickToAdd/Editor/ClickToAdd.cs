#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class ClickToAdd : EditorWindow {

    //Version 1.0 of click to add
    //Initial Release
    //Adds prefab with the click of a mouse button to the scene view
    //Option to randomly rotate objects on each axis
    //Will mass place randomly based on radius of circle and number of prefabs to place.
    //Optional setting to set parent of prefab on instantiation
    //Parenting is mostly done to help keep things organized
    //There is also the option to align vertical axis with the normal of the target

    //Version 1.1 
    //Fixed small error that caused Unity to crash when CTA was left open and Unity was closed

    //Version 1.2
    //Added the ability to spawn multiple prefabs
    //Added "weights" to prefab to adjust the randomness of placement
    //Added limits to the rotation around each axis

    //version 1.3
    //Added "offset" to allow prefabs to be offset vertically - credit to @PolyscapeStudio for the idea!
    //Added random offset to allow random offsets
    //Added feature so that right clicking in the scene view will now cancel placement
    //Clean up code
    //Small formatting changes

    private UnityEngine.Object assetToPlace = null;
    private UnityEngine.Object assetParent = null;
    private bool autoParent = true;
    private bool asPrefab = true;
    private int newNumPrefab = 1;

    private List<prefabToAdd> preFabList = new List<prefabToAdd>();
    private List<float> preFabWeights = new List<float>();

    private bool useOffset = false;
    private bool useRandomOffSet = false;
    private bool OffSetScaleCompensate = false;
    private float offSetAmount = 0.0f;
    private float minOffset = 0f;
    private float maxOffset = 0.2f;
    private Vector3 tempPointHolder;

    private bool rotateX = false;
    private bool rotateY = false;
    private bool rotateZ = false;
    private float rotationX = 180f;
    private float rotationY = 180f;
    private float rotationZ = 180f;

    private bool scalePrefab = false;
    private bool scaleInteger = true;
    private bool scaleEven = true;
    private float xScaleMin = 1f;
    private float yScaleMin = 1f;
    private float zScaleMin = 1f;
    private float xScaleMax = 1f;
    private float yScaleMax = 1f;
    private float zScaleMax = 1f;

    private bool placing = false;

    private bool alignToNormal = false;
    private bool massPlace = false;
    private int numberToPlace = 10;
    private float radiusToPlace = 5.0f;

    private bool useHeightDifference = false;
    private float heightDifference = 5f;
    private bool useAngleDifference = false;
    private float normalDifference = 20f;
    private bool useClustering = false;
    private float clustering = 0f;
    private float _currentCluster;
    private Vector3 _lastPosition;

    private bool usePerlinNoise = false;
    private int seed = 0000;
    private int _seed;
    private int octaves = 2;
    private int _octaves;
    private float damping = 1.5f;
    private float _damping;
    private float lacunarity = 2f;
    private float _lacunarity;
    private float noiseThreshold = 0.5f;
    private bool showNoisePreview = true;
    private bool invertNoise = false;
    private float[,] noiseMap;
    private Texture2D noiseTexture;

    private Vector2 scrollPos;

    private Ray ray;
    private RaycastHit hit;
    private GUISkin skin;

    private GameObject massPlacePrefab;
    private Projector massPlaceProjector;
    private GameObject massPlaceInstance;

	[MenuItem ("Window/Click To Add")]
	public static void  ShowWindow () {
		EditorWindow.GetWindow(typeof(ClickToAdd));
	}
	
	void OnEnable()
	{
		hideFlags = HideFlags.HideAndDontSave;

        //subscribe to onSceneGUI
        SceneView.duringSceneGui += SceneGUI;
		
		//Initialize prefablist
		if(preFabList.Count == 0)
		{
			prefabToAdd tempPTA = new prefabToAdd();
			preFabList.Add (tempPTA);
		}

		//ensure that number of slots displayed matches list size
		newNumPrefab = preFabList.Count;

        skin = EditorGUIUtility.Load("Assets/ClickToAdd/Resources/ClickToAdd.guiskin") as GUISkin;
        massPlacePrefab = EditorGUIUtility.Load("Assets/ClickToAdd/Resources/MassPlaceIndicator.prefab") as GameObject;
        noiseTexture = EditorGUIUtility.Load("Assets/ClickToAdd/Resources/NoiseTexture.png") as Texture2D;
        //massPlaceProjector = massPlacePrefab.GetComponent<Projector>();

        //if (massPlaceProjector == null)
        //    Debug.Log("Didn't find projector");
        //else
        //    GetMassPlacementIndicator();
    }
	
	void OnDisable()
	{
		SceneView.duringSceneGui -= SceneGUI;
        DestroyMassPlaceIndicator();
	}

    void OnGUI()
    {
        EditorGUILayout.Space();
        if (skin != null)
            GUILayout.Box("Click To Add", skin.GetStyle("Heading"));

        if (Application.isPlaying)
        {
            //SceneView.onSceneGUIDelegate -= SceneGUI;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("I get grumpy in play mode so I'm taking a break...");

            if(massPlaceProjector != null)
                DestroyMassPlaceIndicator();
            return;
        }

        if(placing)
            ResizeMassPlaceIndicator();

        if (usePerlinNoise)
            UpdateNoiseTexture();
      

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.HelpBox("Shift + Left Click to Toggle Placing On", MessageType.None);
        EditorGUILayout.HelpBox("Shift + Right Click to Toggle Placing Off", MessageType.None);
        EditorGUILayout.Space();
        

        //	Check if list is full
        //	adds to list of prefabs
        // Will only occur when GUI repaints to avoid errors
        if (Event.current.type == EventType.Layout)
        {
            int lastIndex;
            lastIndex = preFabList.Count - 1;

            if (preFabList[lastIndex].prefab != null)
                newNumPrefab++;
        }

        //dynamically add to prefab list
        for (int i = 0; i < newNumPrefab; i++)
        {

            prefabToAdd tempPTA = new prefabToAdd();

            if (preFabList.Count != newNumPrefab)
            {
                if (Event.current.type == EventType.Layout)
                    preFabList.Add(tempPTA);
            }

            if (preFabList.Count == newNumPrefab)
            {

                string fieldLabel;
                int tempInt = i + 1;
                fieldLabel = "Prefab # " + tempInt;

                preFabList[i].prefab = EditorGUILayout.ObjectField(fieldLabel, preFabList[i].prefab, typeof(GameObject), false) as GameObject;

                EditorGUI.indentLevel++;
                preFabList[i].weight = EditorGUILayout.Slider("Weight", preFabList[i].weight, 0f, 1f);
                //preFabList[i].connected = EditorGUILayout.ToggleLeft("Connected Prefab", preFabList[i].connected);
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.Space();
        if (GUILayout.Button("Clear Prefabs"))
        {
            newNumPrefab = 1;
            preFabList = new List<prefabToAdd>();
            preFabList.Add(new prefabToAdd());
        }


        EditorGUILayout.Space();
        GUILayout.BeginVertical();
        EditorGUILayout.LabelField("Weight Settings");
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Zero"))
            ZeroWeights();
        if (GUILayout.Button("Equal"))
            EqualWeghts();

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Custom"))
            SaveCustomWeights();
        if (GUILayout.Button("Use Custom"))
            UseCustomWeights();

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        //Remove extra rows from prefab lists
        if (Event.current.type == EventType.Repaint)
        {
            int lastIndex;
            lastIndex = preFabList.Count - 1;

            if (preFabList.Count > 1)
            {
                if (preFabList[lastIndex - 1].prefab == null && preFabList[lastIndex].prefab == null)
                {
                    newNumPrefab--;
                    preFabList.RemoveAt(lastIndex);
                }
            }
        }
        EditorGUILayout.Space();
        autoParent = EditorGUILayout.ToggleLeft("Set Parent to Target", autoParent);
        EditorGUILayout.Space();
        EditorGUI.indentLevel++;
        assetParent = EditorGUILayout.ObjectField("Parent (Optional)", assetParent, typeof(GameObject), true);
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
        asPrefab = EditorGUILayout.ToggleLeft("Instantiate as connected Prefab", asPrefab);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Randomly Rotate Asset");
        EditorGUI.indentLevel++;
        rotateX = EditorGUILayout.BeginToggleGroup("X axis", rotateX);
        EditorGUI.indentLevel++;
        rotationX = EditorGUILayout.Slider("Rotation Limit", rotationX, 0f, 180f);
        EditorGUI.indentLevel--;
        EditorGUILayout.EndToggleGroup();
        rotateY = EditorGUILayout.BeginToggleGroup("Y axis", rotateY);
        EditorGUI.indentLevel++;
        rotationY = EditorGUILayout.Slider("Rotation Limit", rotationY, 0f, 180f);
        EditorGUI.indentLevel--;
        EditorGUILayout.EndToggleGroup();
        rotateZ = EditorGUILayout.BeginToggleGroup("Z axis", rotateZ);
        EditorGUI.indentLevel++;
        rotationZ = EditorGUILayout.Slider("Rotation Limit", rotationZ, 0f, 180f);
        EditorGUI.indentLevel--;
        EditorGUILayout.EndToggleGroup();
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Randomly Scale Prefab");
        EditorGUI.indentLevel++;
        scalePrefab = EditorGUILayout.BeginToggleGroup("Scale Prefab", scalePrefab);
        EditorGUI.indentLevel++;
        if (scalePrefab)
        {
            scaleInteger = EditorGUILayout.ToggleLeft("Use Integer Scaling", scaleInteger);
            scaleEven = EditorGUILayout.ToggleLeft("Scale Evenly", scaleEven);
            if(scaleEven)
            {
                xScaleMin = EditorGUILayout.Slider("Scale Min", xScaleMin, 1f, 10f);                
                xScaleMax = EditorGUILayout.Slider("Scale Max", xScaleMax, 1f, 10f);                
            }
            else
            {
                xScaleMin = EditorGUILayout.Slider("X Scale Min", xScaleMin, 1f, 10f);
                xScaleMax = EditorGUILayout.Slider("X Scale Max", xScaleMax, 1f, 10f);
                EditorGUILayout.Space();
                yScaleMin = EditorGUILayout.Slider("Y Scale Min", yScaleMin, 1f, 10f);
                yScaleMax = EditorGUILayout.Slider("Y Scale Max", yScaleMax, 1f, 10f);
                EditorGUILayout.Space();
                zScaleMin = EditorGUILayout.Slider("Z Scale Min", zScaleMin, 1f, 10f);
                zScaleMax = EditorGUILayout.Slider("Z Scale Max", zScaleMax, 1f, 10f);                    
            }

            //Scale to integers
            if (scaleInteger)
            {
                xScaleMin = Mathf.RoundToInt(xScaleMin);
                xScaleMax = Mathf.RoundToInt(xScaleMax);
                yScaleMin = Mathf.RoundToInt(yScaleMin);
                yScaleMax = Mathf.RoundToInt(yScaleMax);
                zScaleMin = Mathf.RoundToInt(zScaleMin);
                zScaleMax = Mathf.RoundToInt(zScaleMax);
            }

        }
        EditorGUILayout.EndToggleGroup();
        EditorGUI.indentLevel--;
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Offset prefabs from ground level");
        EditorGUI.indentLevel++;
        useOffset = EditorGUILayout.BeginToggleGroup("Use Offset", useOffset);

        if (useOffset)
        { 
            EditorGUI.indentLevel++;
            offSetAmount = EditorGUILayout.Slider("Depth", offSetAmount, -10f, 10f);
            EditorGUI.indentLevel--;
            useRandomOffSet = EditorGUILayout.BeginToggleGroup("Use Random Depth", useRandomOffSet);
            EditorGUI.indentLevel++;
            maxOffset = EditorGUILayout.Slider("Max Offset", maxOffset, -10f, 10f);
            minOffset = EditorGUILayout.Slider("Min Offset", minOffset, -10f, 10f);
            EditorGUILayout.EndToggleGroup();
            EditorGUI.indentLevel--;         
            OffSetScaleCompensate = EditorGUILayout.ToggleLeft(new GUIContent("Adjust Offset for Scale (Experimental)", "Attempts to adjust for origin of object not matching origin of mesh (vertically)"), OffSetScaleCompensate);

        }
        EditorGUILayout.EndToggleGroup();
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
		EditorGUILayout.LabelField("Align Prefab to Target Normal");
		EditorGUI.indentLevel++;
		alignToNormal = EditorGUILayout.BeginToggleGroup("Rotate Y-axis to Normal", alignToNormal);
        EditorGUILayout.EndToggleGroup();
        EditorGUI.indentLevel--;
		EditorGUILayout.Space();

        //Mass Place UI
        EditorGUILayout.LabelField("Option to Mass Place Prefabs");
		EditorGUI.indentLevel++;		
		massPlace = EditorGUILayout.BeginToggleGroup("Mass Place", massPlace);
        if (massPlace)
        {
            EditorGUILayout.HelpBox("Shift + MSW to change radius", MessageType.None);
            EditorGUILayout.HelpBox("Crtl + MSW to change number to place", MessageType.None);
        }
        EditorGUI.indentLevel++;
        //Radius in which to instantiate prefabs
        if (massPlace)
        {
            radiusToPlace = EditorGUILayout.Slider("Radius To Place In", radiusToPlace, 1f, 250f);
            //Adjust max number of placements portional to area
            int maxToPlace;
            maxToPlace = Mathf.RoundToInt(radiusToPlace * radiusToPlace) / 2;
            //Number of prefabs to instantiate
            numberToPlace = EditorGUILayout.IntSlider("Number To Place", numberToPlace, 1, maxToPlace);
            EditorGUI.indentLevel--;
            useHeightDifference = EditorGUILayout.BeginToggleGroup("Use Height Matching", useHeightDifference);
            EditorGUI.indentLevel++;
            heightDifference = EditorGUILayout.Slider("Height Difference", heightDifference, 0f, 100f);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
            useAngleDifference = EditorGUILayout.BeginToggleGroup("Use Angle Matching", useAngleDifference);
            EditorGUI.indentLevel++;
            normalDifference = EditorGUILayout.Slider("Max Angle Difference", normalDifference, 0f, 90f);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
            useClustering = EditorGUILayout.BeginToggleGroup("Use Clustering", useClustering);
            EditorGUI.indentLevel++;
            clustering = EditorGUILayout.Slider("Clustering", clustering, 0f, 20f);
            
            EditorGUILayout.EndToggleGroup();
            EditorGUI.indentLevel--;

            usePerlinNoise = EditorGUILayout.BeginToggleGroup("Use Perlin Noise (Experimental)", usePerlinNoise);
            EditorGUI.indentLevel++;
            noiseThreshold = EditorGUILayout.Slider("Placement Threshold", noiseThreshold, 0f, 1f);
            invertNoise = EditorGUILayout.ToggleLeft("Invert Noise", invertNoise);
            EditorGUILayout.LabelField("Noise Creation Settings");
            EditorGUI.indentLevel++;
            octaves = EditorGUILayout.IntSlider("Octaves", octaves, 1, 8);
            damping = EditorGUILayout.Slider("Damping", damping, 0.01f, 5f);
            lacunarity = EditorGUILayout.Slider("Lacunarity", lacunarity, 1f, 5f);
            seed = EditorGUILayout.IntField("Random Seed", seed);
            showNoisePreview = EditorGUILayout.ToggleLeft("Show Noise Preview (Slow)", showNoisePreview);
            EditorGUI.indentLevel--;

            if (showNoisePreview && usePerlinNoise)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (skin != null)
                    GUILayout.Box("Hello ", skin.GetStyle("PerlinTexture"));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();

        }
        EditorGUILayout.EndToggleGroup();
		EditorGUI.indentLevel--;

        if (!placing)
		{
			if(GUILayout.Button("Start Placing"))
				placing = true;
		}
		else
		{
            if (GUILayout.Button("Stop Placing"))
            {
                placing = false;
                DestroyMassPlaceIndicator();
            }
		}
		
		EditorGUILayout.Space ();
        EditorGUILayout.EndScrollView();		
	}
	
	void SceneGUI(SceneView sceneView)
	{
		if(placing)
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
		else
			HandleUtility.Repaint();

        if (placing)
        {            
            ResizeMassPlaceIndicator();
            MoveMassPlaceIndicator();
        }

        ToggleMassPlaceIndicator();
        ShiftKeyControls();

        if (preFabList.Count != 0)
		{
            if (placing && preFabList[0].prefab != null)
			{
				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
	
				if (Physics.Raycast(ray, out hit, 1000.0f)) 
				{
					if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
					{
						//Check if mass placing
						if(massPlace)
						{
                            if (useClustering)
                            {
                                _lastPosition = Vector3.zero;
                                _currentCluster = clustering; //reset with each mass placement
                            }

							for(int i = 0; i < numberToPlace; i++)
							{
                                bool canPlace = true;
								Vector3 tempPos;
                                tempPos = GetMassPlacePosition(hit);

                                if (useHeightDifference)
                                {
                                    if (tempPos.y > hit.point.y + heightDifference || tempPos.y < hit.point.y - heightDifference)
                                        canPlace = false;
                                }
                                
                                if(usePerlinNoise && canPlace)
                                {
                                    float noise = GetNoiseValue(tempPos.x, tempPos.y);
                                    if (invertNoise)
                                        noise = 1f - noise;

                                    if (noise > noiseThreshold)
                                        canPlace = false;
                                }
                              
                                if (useOffset && canPlace)
                                {
                                    if (useRandomOffSet)
                                        offSetAmount = UnityEngine.Random.Range(maxOffset, minOffset);
                                    tempPos += Vector3.up * offSetAmount;
                                }
															
								//If position returned is valid
								if(tempPos != Vector3.zero && canPlace)
								{
									GameObject placedGO;                          
                                    placedGO = PlaceAsset(tempPos, hit.transform.gameObject);
                                    float verticalScale = 0f; // used for offset "correction"

                                    //scale prefab
                                    if (scalePrefab && scaleEven)
                                    {
                                        float scale = 0f;

                                        if (scaleInteger)
                                            scale = Mathf.RoundToInt(UnityEngine.Random.Range(xScaleMin, xScaleMax));
                                        else
                                            scale = UnityEngine.Random.Range(xScaleMin, xScaleMax);

                                        placedGO.transform.localScale *= scale;
                                        verticalScale = scale;
                                    }
                                    else if (scalePrefab)
                                    {
                                        Vector3 scale = placedGO.transform.localScale;
                                        if (scaleInteger)
                                        {
                                            scale = new Vector3(scale.x * Mathf.RoundToInt(UnityEngine.Random.Range(xScaleMin, xScaleMax)),
                                                    scale.y * Mathf.RoundToInt(UnityEngine.Random.Range(yScaleMin, yScaleMax)),
                                                    scale.z * Mathf.RoundToInt(UnityEngine.Random.Range(zScaleMin, zScaleMax)));
                                        }
                                        else
                                        {
                                            scale = new Vector3(scale.x * UnityEngine.Random.Range(xScaleMin, xScaleMax),
                                                    scale.y * UnityEngine.Random.Range(yScaleMin, yScaleMax),
                                                    scale.z * UnityEngine.Random.Range(zScaleMin, zScaleMax));
                                        }
                                        placedGO.transform.localScale = scale;
                                        verticalScale = scale.y;
                                    }

                                    //align prefab with normal of object hit by raycast
                                    if (alignToNormal)
										AlignWithNormal(hit, placedGO);

                                    if (OffSetScaleCompensate && scalePrefab)
                                    {
                                        float deltaOffSet = offSetAmount * verticalScale;
                                        placedGO.transform.position += new Vector3(0, deltaOffSet, 0);

                                    }
                                }

                                
                            }
						}
						else
						{
							GameObject placedGO;
                            tempPointHolder = hit.point;
                            if (useOffset == true)
                            {
                                if (useRandomOffSet)
                                    offSetAmount = UnityEngine.Random.Range(maxOffset, minOffset);
                                tempPointHolder += Vector3.up * offSetAmount;
                            }

                            placedGO = PlaceAsset(tempPointHolder, hit.transform.gameObject);
                            float verticalScale = 0f; // used for offset "correction"
                            //scale prefab
                            if (scalePrefab && scaleEven)
                            {
                                float scale = 0f;

                                if(scaleInteger)
                                    scale = Mathf.RoundToInt(UnityEngine.Random.Range(xScaleMin, xScaleMax));
                                else
                                    scale = UnityEngine.Random.Range(xScaleMin, xScaleMax);

                                placedGO.transform.localScale *= scale;
                                verticalScale = scale;
                            }
                            else if (scalePrefab)
                            {
                                Vector3 scale = placedGO.transform.localScale;
                                if (scaleInteger)
                                {
                                    scale = new Vector3(scale.x * Mathf.RoundToInt(UnityEngine.Random.Range(xScaleMin, xScaleMax)),
                                            scale.y * Mathf.RoundToInt(UnityEngine.Random.Range(yScaleMin, yScaleMax)),
                                            scale.z * Mathf.RoundToInt(UnityEngine.Random.Range(zScaleMin, zScaleMax)));
                                }
                                else
                                {
                                    scale = new Vector3(scale.x * UnityEngine.Random.Range(xScaleMin, xScaleMax),
                                            scale.y * UnityEngine.Random.Range(yScaleMin, yScaleMax),
                                            scale.z * UnityEngine.Random.Range(zScaleMin, zScaleMax));
                                }
                                placedGO.transform.localScale = scale;

                                
                                verticalScale = scale.y;
                            }

                            //align prefab with normal of object hit by raycast
                            if (alignToNormal)
								AlignWithNormal(hit, placedGO);

                            if (OffSetScaleCompensate && scalePrefab)
                            {
                                float deltaOffSet = offSetAmount * verticalScale - offSetAmount;
                                placedGO.transform.position += new Vector3(0, deltaOffSet, 0);

                            }
                        }
						
					}
				}
			}
		}
	}
		
	GameObject PlaceAsset( Vector3 spawnPos, GameObject hitObject)
	{
		assetToPlace = ChooseAsset();
		//Debug.Log(assetToPlace.name);
	
		GameObject tempAsset;
		float rotX;
		float rotY;
		float rotZ;
		
		//Get Random Rotatation values
		if(rotateX)
			rotX = UnityEngine.Random.Range(-rotationX,rotationX);
		else
			rotX = 0f;
		
		if(rotateY)
			rotY = UnityEngine.Random.Range(-rotationY, rotationY);
		else
			rotY = 0f;
		
		if(rotateZ)
			rotZ = UnityEngine.Random.Range(-rotationZ, rotationZ);
		else
			rotZ = 0f;

		//Instantiate Object
		if(asPrefab)
		{
			tempAsset = PrefabUtility.InstantiatePrefab(assetToPlace) as GameObject;
			tempAsset.transform.position = spawnPos;
			Undo.RegisterCreatedObjectUndo(tempAsset, "Created GO");
		}
		else
		{
			tempAsset = Instantiate((GameObject)assetToPlace,spawnPos,Quaternion.identity) as GameObject;
			tempAsset.name = assetToPlace.name;
			Undo.RegisterCreatedObjectUndo(tempAsset, "Created GO");
		}

		//Rotation Object
		Undo.RecordObject(tempAsset.transform, "Change Rotation");
		tempAsset.transform.Rotate(rotX, rotY, rotZ);

		//Set parent
		if(assetParent != null && !autoParent)
		{
			GameObject tempGO;
			tempGO = (GameObject) assetParent;
			Undo.SetTransformParent(tempAsset.transform, tempGO.transform, "Set Parent to User Choice");
			tempAsset.transform.parent = tempGO.transform;	
		}
		if(autoParent)
		{
			Undo.SetTransformParent(tempAsset.transform, hitObject.transform, "Set Parent to User Choice");
			tempAsset.transform.parent = hitObject.transform;
		}
		
		return tempAsset; 		
	}
	
	Vector3 GetMassPlacePosition(RaycastHit target)
	{
		//Get random position as a function of radius
		Vector2 randV2;

        randV2 = UnityEngine.Random.onUnitSphere * radiusToPlace;
        
        if(useClustering)
        {
            if (_lastPosition == Vector3.zero)
                _lastPosition = randV2; 

            _currentCluster += (clustering);            
            randV2 = UnityEngine.Random.onUnitSphere * _currentCluster + _lastPosition;

            //distance check. Reset _lastposition if outside radius
            float distance = randV2.sqrMagnitude;
            if (distance > radiusToPlace * radiusToPlace)
            {
                randV2 = UnityEngine.Random.onUnitSphere * radiusToPlace;
                _currentCluster = clustering;
                _lastPosition = randV2;
            }
                 
        }

        //Location of mouse click in scene		
        Vector3 centerPos;
		centerPos = target.point;
		
		//Raycast to get height at new random position
		float height;
		Ray tempRay;
		RaycastHit rayHit;
		Vector3 rayOrigin;
		rayOrigin = centerPos + new Vector3(randV2.x, 0, randV2.y) + new Vector3(0,100,0);
		tempRay = new Ray(rayOrigin, -Vector3.up);
		
		Vector3 newPosition = Vector3.zero;
		
		//Raycast to get position of mass placed prefab
		if(Physics.Raycast (tempRay, out rayHit))
		{
            if (useAngleDifference) // check if normals match
            {
                float angleDifference = Vector3.Angle(Vector3.up, target.normal) - Vector3.Angle(Vector3.up, rayHit.normal);
                angleDifference = Mathf.Abs(angleDifference);

                if (angleDifference > normalDifference)
                    return Vector3.zero;
            }

            //Check to that new target is the same as the clicked target
            if (rayHit.transform.name == target.transform.name)
			{
				height = rayHit.point.y;
				//Set new position
				newPosition = new Vector3(centerPos.x, 0, centerPos.z) + new Vector3(randV2.x, height, randV2.y);
			}
			else
			{
				//Returning vector (0,0,0) does not instantiate a new object
				newPosition = Vector3.zero;
			}
		}
		else
		{
			//Returning vector (0,0,0) does not instantiate a new object
			newPosition = Vector3.zero;
		}
		
		return newPosition;
	}
	
	void AlignWithNormal(RaycastHit rayHit, GameObject tempGO)
	{
		tempGO.transform.localRotation = Quaternion.FromToRotation(tempGO.transform.up, rayHit.normal);
	}
	
	//Normalize weights of prefab placements
	void NormalizeWeights()
	{
		float tempFloat = 0f; 
	
		//sum weights
		foreach(prefabToAdd prefab in preFabList)
			tempFloat += prefab.weight;

		foreach(prefabToAdd prefab in preFabList)
			prefab.weight = prefab.weight / tempFloat;
	}
	
	//Choose asset to place based on random number and weighting
	GameObject ChooseAsset()
	{
		GameObject tempGO;
	
		//set default prefab
		//If all weights set to zero CTA will use first in the list
		if(preFabList[0].prefab != null)
			tempGO = preFabList[0].prefab;
		else
			tempGO = new GameObject();
		
		//sum weights
		float sumWeights = 0f; 

		for(int i = 0; i < preFabList.Count; i++)
		{
			if(preFabList[i].prefab != null)
			{
				sumWeights += preFabList[i].weight;
				preFabList[i].lotteryNumber = sumWeights - preFabList[i].weight/2f;
			}
		}
		
		float randomSeed;
		randomSeed = UnityEngine.Random.Range(0f, sumWeights);
		//Debug.Log("Random : " + randomSeed);
		
		for(int i = 0; i < preFabList.Count; i++)
		{		
			if(preFabList[i].prefab != null)
			{
				float tempfloat;
				tempfloat = Mathf.Abs(preFabList[i].lotteryNumber - randomSeed);
				
				if(tempfloat < preFabList[i].weight/2 && preFabList[i].weight != 0)
					tempGO = preFabList[i].prefab;
			}
		}	
		return tempGO;
	}
	
	void ZeroWeights()
	{
		
		int prefabCount;
		prefabCount = preFabList.Count;
		
		for(int i = 0; i < prefabCount; i++)
			preFabList[i].weight = 0f;
	}
	
	void EqualWeghts()
	{
		int prefabCount;
		prefabCount = preFabList.Count;
		
		for(int i = 0; i < prefabCount; i++)
			preFabList[i].weight = 0.5f;
	}
	
	void SaveCustomWeights()
	{
		int prefabCount;
		prefabCount = preFabList.Count;
		preFabWeights.Clear();
		
		for(int i = 0; i < prefabCount; i++)
			preFabWeights.Add(preFabList[i].weight);
	}
	
	void UseCustomWeights()
	{
		int prefabCount;
		prefabCount = preFabList.Count;
		
		if(prefabCount > 0){
			for(int i = 0; i < prefabCount; i++)
				preFabList[i].weight = preFabWeights[i];
		}
	}

    void MoveMassPlaceIndicator()
    {
        Vector3 position;
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit _hit;

        if (Physics.Raycast(ray, out _hit, 1000.0f))
        {
            position = _hit.point;

            if (useHeightDifference)
                position += new Vector3(0f, heightDifference, 0f);
            else
                position += new Vector3(0f, 50f, 0f);

            massPlaceInstance.transform.position = position;
        }

        SceneView.RepaintAll();
    }

    void ResizeMassPlaceIndicator()
    {
        if (massPlaceProjector == null)
        {
            GetMassPlacementIndicator();
        }

        if (massPlace)
            massPlaceProjector.orthographicSize = radiusToPlace * 512f / 500f; //fix for actual size of circle in texture
        else
            massPlaceProjector.orthographicSize = 2f;

        SceneView.RepaintAll();
    }

    void GetMassPlacementIndicator()
    {
        if(massPlaceInstance == null)
        {
            //if(massPlacePrefab == null)
            //{
            //    Debug.LogError("Missing Mass Place Indicator Prefab");
            //    return;
            //}

            massPlaceInstance = GameObject.Find(massPlacePrefab.name);

            if (massPlaceInstance == null)
            {
                massPlaceInstance = (GameObject)Instantiate(massPlacePrefab);
                massPlaceInstance.hideFlags = HideFlags.HideAndDontSave;
                massPlaceInstance.name = massPlacePrefab.name;
            }

            if(massPlaceProjector == null || massPlaceProjector.gameObject != massPlaceInstance)
                massPlaceProjector = massPlaceInstance.GetComponent<Projector>();
        }
    }

    void ToggleMassPlaceIndicator()
    {
        if (massPlaceInstance == null)
            return;
        if (placing)
            massPlaceInstance.SetActive(true);
        else
            massPlaceInstance.SetActive(false);
    }

    void ShiftKeyControls()
    {
        if (Event.current.shift && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            placing = true;
            Event.current.Use();
        }

        ////right click cancels placing
        if (Event.current.shift && Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            placing = false;
            DestroyMassPlaceIndicator();
            Event.current.Use();
        }

        if (Event.current.shift && Event.current.type == EventType.ScrollWheel)
        {
            radiusToPlace += Event.current.delta.y;
            if(numberToPlace * 2 > radiusToPlace)
                numberToPlace = Mathf.RoundToInt(numberToPlace / 2);
            Event.current.Use();
            Repaint();
        }

        if (Event.current.control && Event.current.type == EventType.ScrollWheel)
        {
            numberToPlace += Mathf.RoundToInt(Event.current.delta.y);
            Event.current.Use();
            Repaint();
        }

    }

    void UpdateNoiseTexture()
    {
        if (noiseTexture == null)
            Debug.LogWarning("No texure");

        if(damping != _damping || octaves != _octaves || seed != _seed || lacunarity != _lacunarity)
        {
            if (showNoisePreview)
            {
                noiseMap = NoiseMapGenerator.GetNoiseMap(512, octaves, seed, damping, lacunarity);
                if (showNoisePreview && noiseTexture != null)
                    NoiseMapGenerator.DrawMap(noiseTexture, noiseMap);
            }

            _damping = damping;
            _octaves = octaves;
            _seed = seed;
            _lacunarity = lacunarity;
        }             
    }

    //Returns noise value at given coordinates
    float GetNoiseValue(float x, float y)
    {
        return NoiseMapGenerator.GetNoise(octaves, seed, damping, lacunarity, x, y);
    }

    private void DestroyMassPlaceIndicator()
    {
        DestroyImmediate(massPlaceInstance);
    }
}

[System.Serializable]
public class prefabToAdd 
{
	public GameObject prefab;
	public float weight = 0.5f;
	public bool connected = true;
	//used in selection of prefab
	public float lotteryNumber = 0f;
}
#endif