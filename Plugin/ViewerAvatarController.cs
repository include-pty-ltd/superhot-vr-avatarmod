using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using AvatarScriptPack;

namespace Include.VR.Plugin
{
    public class ViewerAvatarController : MonoBehaviour
    {
        GameObject avatarObject;
        GameObject avatarInstance;
        GameObject currentCamera;

        Material pianoBlack;
        
        public Transform head
        {
            get
            {
                _head = _head ?? FindAndReturnFirst(Config.avatarhead);
                GameObject go = new GameObject();
                go.transform.parent = _head;
                StartCoroutine(SetLocalTransforms(go.transform, new Vector3(0, 0, 0), Quaternion.Euler(new Vector3(0, 270, 270))));
                //var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                //obj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                //obj.transform.parent = go.transform;
                //StartCoroutine(SetLocalTransforms(obj.transform));
                _head = go.transform;
                return _head;
            }
            set
            {
                _head = value;
            }
        }
        public Transform rightHand
        {
            get
            {
                _rightHand = _rightHand ?? FindAndReturnFirst(Config.avatarrighthand);
                GameObject go = new GameObject();
                go.transform.parent = _rightHand;
                StartCoroutine(SetLocalTransforms(go.transform, new Vector3(0, 0, 0), Quaternion.Euler(new Vector3(270, 90, 10))));
                _rightHand = go.transform;
                return _rightHand;
            }
            set
            {
                _rightHand = value;
            }
        }
        public Transform leftHand
        {
            get
            {
                _leftHand = _leftHand ?? FindAndReturnFirst(Config.avatarlefthand);
                GameObject go = new GameObject();
                go.transform.parent = _leftHand;
                StartCoroutine(SetLocalTransforms(go.transform, new Vector3(-0.05f, 0, 0), Quaternion.Euler(new Vector3(270, 180, 10))));
                _leftHand = go.transform;
                return _leftHand;
            }
            set
            {
                _leftHand = value;
            }
        }
        public Transform space
        {
            get
            {
                _space = _space ?? FindAndReturnFirst(Config.avatarspace);
                return _space;
            }
            set
            {
                _space = value;
            }
        }

        private Transform _head;
        private Transform _leftHand;
        private Transform _rightHand;
        private Transform _space;

        // Use this for initialization
        private void Awake()
        {
            DontDestroyOnLoad(this);
            //load the path
            string path = Path.Combine(Application.streamingAssetsPath, "avatar.asset");
            if (!File.Exists(path))
            {
                Plugin.Log("could not find the path (" + path + ")");
                Destroy(gameObject);
                return;
            }

            //load the bundle
            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                Plugin.Log("Path was: " + path);
                Plugin.Log("bundle was null");
                Destroy(gameObject);
                return;
            }

            avatarObject = bundle.LoadAsset<GameObject>("avatar.prefab");
            if (avatarObject == null)
            {
                Plugin.Log("Avatar was missing from the asset file");
                Destroy(gameObject);
                return;
            }

            if (avatarObject.GetComponent<VRIK>() == null)
            {
                Plugin.Log("Failed to find final ik");
                Destroy(gameObject);
                return;
            }

            SceneManager.sceneLoaded += SceneLoadedListener;
            UpdateTrackingReferences();

            Plugin.Log("End Awake");
            //configure the vr camera and detect if it's changed
        }
        
        private Transform FindAndReturnFirst(string[] names)
        {
            Transform result = null;
            foreach (String name in names)
            {
                Plugin.Log("Searching for " + name);
                //see if we can find the target
                result = GameObject.Find(name)?.transform;
                if (result != null)
                { // we found a target and now we're quitting out
                    Plugin.Log("Found " + name);
                    return result;
                }
            }
            // we didn't find a target
            return result;
        }

        IEnumerator CheckIKTargets(VRIK ik, IKManager ikman, GameObject avatar)
        {
            yield return 0;
            while (ik != null && ikman != null)
            {

                if (ik.solver.spine.headTarget == null ||
                    ik.solver.rightArm.target == null ||
                    ik.solver.leftArm.target == null)
                {
                    avatar.transform.parent = space ?? transform;
                    Plugin.Log("vrik values were null!");
                    ik.solver.spine.headTarget = head;
                    //ikman.RightHandTarget = rightHand;
                    ik.solver.rightArm.target = rightHand;
                    //ikman.LeftHandTarget = leftHand;
                    ik.solver.leftArm.target = leftHand;
                    Plugin.Log("vrik values have been set!");
                }
                yield return new WaitForSecondsRealtime(1);
            }
            Plugin.Log("vrik or ikman was null, quitting!");
        }

        IEnumerator SetLocalTransforms(Transform t, Vector3 pos = new Vector3(), Quaternion rot = new Quaternion())
        {
            yield return 0;
            t.localPosition = pos;
            t.localRotation = rot;
        }

        IEnumerator SetMaterialsForDude(SkinnedMeshRenderer smr)
        {
            yield return 0;
            Material m = GameObject.Find("hands:Lhand")?.GetComponentInChildren<Renderer>()?.material;
            Plugin.Log(m == null ? "no material" : "found material");
            Material[] mats = smr.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = new Material(m ?? pianoBlack);
            }
            smr.materials = mats;
            Plugin.Log("Materials set for super hot");
        }

        private void SetChildrenToLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (var child in go.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = layer;
                //GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //part.transform.parent = child;
                //StartCoroutine(ZeroTransforms(part.transform));
                //part.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            }
        }

        private void CreateAvatar()
        {
            Plugin.Log("Creating a new avatar");
            avatarInstance = Instantiate(avatarObject, space ?? this.transform);
            DontDestroyOnLoad(avatarInstance);
            avatarInstance.name = "Avatar";
            StartCoroutine(SetLocalTransforms(avatarInstance.transform));
            SetChildrenToLayer(avatarInstance, Config.avatarlayer);

            VRIK vrik = avatarInstance.GetComponent<VRIK>();
            if (vrik == null)
            {
                Plugin.Log("Failed to find final ik but it was found previously for some reason");
                return;
            }
            IKManager ikmanager = avatarInstance.GetComponent<IKManager>();
            StartCoroutine(CheckIKTargets(vrik, ikmanager, avatarInstance));

            Plugin.Log("Setting VRIK bones");
            vrik.references.root = avatarInstance.transform;
            Transform[] transforms = avatarInstance.GetComponentsInChildren<Transform>();
            Plugin.Log($"Length of transforms is {transforms.Length}");
            foreach (Transform t in transforms)
            {
                switch (t.gameObject.name)
                {
                    case "Bip001 Pelvis":
                        vrik.references.pelvis = t;
                        break;
                    case "Bip001 Spine":
                        vrik.references.spine = t;
                        break;
                    case "Bip001 L Thigh":
                        vrik.references.leftThigh = t;
                        break;
                    case "Bip001 L Calf":
                        vrik.references.leftCalf = t;
                        break;
                    case "Bip001 L Foot":
                        vrik.references.leftFoot = t;
                        break;
                    case "Bip001 L Toe0":
                        vrik.references.leftToes = t;
                        break;
                    case "Bip001 R Thigh":
                        vrik.references.rightThigh = t;
                        break;
                    case "Bip001 R Calf":
                        vrik.references.rightCalf = t;
                        break;
                    case "Bip001 R Foot":
                        vrik.references.rightFoot = t;
                        break;
                    case "Bip001 R Toe0":
                        vrik.references.rightToes = t;
                        break;
                    case "Bip001 Spine1":
                        vrik.references.chest = t;
                        break;
                    case "Bip001 Neck":
                        vrik.references.neck = t;
                        break;
                    case "Bip001 Head":
                        vrik.references.head = t;
                        break;
                    case "Bip001 L Clavicle":
                        vrik.references.leftShoulder = t;
                        break;
                    case "Bip001 L UpperArm":
                        vrik.references.leftUpperArm = t;
                        break;
                    case "Bip001 L Forearm":
                        vrik.references.leftForearm = t;
                        break;
                    case "Bip001 L Hand":
                        vrik.references.leftHand = t;
                        break;
                    case "Bip001 R Clavicle":
                        vrik.references.rightShoulder = t;
                        break;
                    case "Bip001 R UpperArm":
                        vrik.references.rightUpperArm = t;
                        break;
                    case "Bip001 R Forearm":
                        vrik.references.rightForearm = t;
                        break;
                    case "Bip001 R Hand":
                        vrik.references.rightHand = t;
                        break;
                    default:
                        break;
                }
            }

            if (!vrik.references.isFilled) Plugin.Log("Failed to populate vrik's references");
            else Plugin.Log("Successfully populated references");

            //for superhot
            var smr = avatarInstance.GetComponentInChildren<SkinnedMeshRenderer>();
            Shader sh = Shader.Find("XULM/Generic Crystal 0.8 postbeta 2 sided");
            if (smr != null && sh != null)
            {
                Plugin.Log("We super hot now bois!");
                StartCoroutine(SetMaterialsForDude(smr));
            }
        }

        IEnumerator ShowTransform(string name)
        {
            yield return 0;
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                cube.transform.parent = go.transform;
                StartCoroutine(SetLocalTransforms(cube.transform));
            }
        }

        private void ConfigureHMDCamera()
        {
            Plugin.Log("Configuring HMD Camera");
            Camera cam = null;
            if (Config.avatarhmdcamera.Length == 1 && Config.avatarhmdcamera[0] == "Camera.main")
            {
                cam = Camera.main;
                currentCamera = cam?.gameObject;
                if (currentCamera == null)
                {
                    Plugin.Log("Camera is null :( this game doesn't use Camera.main apparently");
                    return;
                }
                Plugin.Log("Found Camera.main");
            }
            else if (Config.avatarhmdcamera.Length > 0)
            {
                currentCamera = FindAndReturnFirst(Config.avatarhmdcamera)?.gameObject;
                cam = currentCamera?.GetComponent<Camera>();
                if (currentCamera == null)
                {
                    StringBuilder sb = new StringBuilder();

                    foreach (string str in Config.avatarhmdcamera)
                        sb.Append(str).Append(" ");

                    sb.Remove(sb.Length - 1, 1);
                    Plugin.Log($"All the cameras that you listed in the config file ({sb.ToString()}) weren't found in the scene!");
                    return;
                }
                Plugin.Log("Found a camera from the list");
            }
            else
            {
                Plugin.Log(@"No cameras to search for, I guess I die then ¯\_(ツ)_/¯");
                return;
            }

            if (cam == null)
            {
                Plugin.Log("we apparently made it this far without quitting out but we still don't have a camera object");
            }

            int mask = cam.cullingMask;
            StringBuilder maskString = new StringBuilder();
            for (int i = 0; i < 32; i++)
            {
                maskString.Append((mask >> i) & 1);
            }
            Plugin.Log($"Current mask is     {maskString.ToString()}");

            maskString.Length = 0;
            for (int i = 0; i < 32; i++)
            {
                maskString.Append(i == Config.avatarlayer ? "^" : " ");
            }

            Plugin.Log($"Culling this layer  {maskString}");
            cam.cullingMask &= ~(1 << Config.avatarlayer);

            mask = cam.cullingMask;
            maskString.Length = 0;
            for (int i = 0; i < 32; i++)
            {
                maskString.Append((mask >> i) & 1);
            }
            Plugin.Log($"Current mask is now {maskString.ToString()}");

        }

        private void SceneLoadedListener(Scene scene, LoadSceneMode mode)
        {
            UpdateTrackingReferences();

            if (pianoBlack == null)
            {
                Plugin.Log("Searching for piano blaq");
                Renderer[] renderers = FindObjectsOfType<Renderer>();
                foreach (var r in renderers)
                {
                    if (r.material.name == "MAIN MEDIUM Dark Crystal Pianoblack MEDIUM SIZED (Instance)")
                    {
                        pianoBlack = new Material(r.material);
                        Plugin.Log("we found piano blac");
                        break;
                    }
                }
            }

            if (avatarInstance == null)
            {
                Plugin.Log("Previous avatar destroyed, making a new one");
                CreateAvatar();
            }

            if (currentCamera == null)
            {
                ConfigureHMDCamera();
            }
        }

        private void UpdateTrackingReferences()
        {
            head = FindAndReturnFirst(Config.avatarhead);
            leftHand = FindAndReturnFirst(Config.avatarlefthand);
            rightHand = FindAndReturnFirst(Config.avatarrighthand);
            space = FindAndReturnFirst(Config.avatarspace);
        }
    }
}