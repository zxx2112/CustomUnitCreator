using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityModManagerNet;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using Landfall.TABS;

namespace CustomUnitCreator
{
    public class Main
    {
        public static UnityModManager.ModEntry _modEntry;
        public static GameObject unitRoot;
        static bool Load(UnityModManager.ModEntry modEntry) {
            _modEntry = modEntry;
            //管理器中创建一个按钮可以进入新场景,场景中实例化一个球体,也许还需要个相机
            modEntry.OnGUI += OnGUI;
            //注册场景切换监听
            SceneManager.activeSceneChanged += OnSceneChanged;
            SceneManager.sceneUnloaded += OnSceneUnLoad;
            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry) {
            if(GUILayout.Button("Enter Unit Creator Scene")) {
                var unitCreatorScene = SceneManager.CreateScene("UnitCreator");
                SceneManager.SetActiveScene(unitCreatorScene);
                //不能立即生成物体,得等待场景切换
                Time.timeScale = 1;
            }
        }

        static void OnSceneChanged(Scene oldScene,Scene newScene) {
            _modEntry.Logger.Log("检测到场景切换 "+oldScene.name+"-->"+newScene.name);
            SceneManager.UnloadSceneAsync(oldScene);

        }

        static void OnSceneUnLoad(Scene unloadedScene) {
            _modEntry.Logger.Log("检测到到场景卸载 " + unloadedScene.name);
            if (unloadedScene.name == "MainMenu" && SceneManager.GetActiveScene().name == "UnitCreator") {
                //创建一个相机
                _modEntry.Logger.Log("-创建相机");
                var camera = new GameObject("CustomMainCamera").AddComponent<Camera>();
                var rigid = camera.gameObject.AddComponent<Rigidbody>();
                rigid.useGravity = false;
                rigid.freezeRotation = true;
                rigid.isKinematic = true;
                camera.gameObject.AddComponent<SphereCollider>();
                camera.gameObject.AddComponent<CameraController>();
                camera.transform.position = new Vector3(0, 1, -10);
                //创建一个灯光
                _modEntry.Logger.Log("-创建灯光");
                var light = new GameObject("CustomLight").AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.position = new Vector3(0, 3, 0);
                light.transform.localEulerAngles = new Vector3(50, -30, 0);
                //创建一个地面
                _modEntry.Logger.Log("-创建地面");
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(100, 1, 100);
                //尝试创建一个小人,基础
                //获取蓝图,然后调用Spawn
                _modEntry.Logger.Log("-创建默认单位");
                var unitDatabase = LandfallUnitDatabase.GetDatabase();
                Unit unit = null;
                unitDatabase.m_unitEditorBlueprint.Spawn(new Vector3(0,2,0), Quaternion.identity, Team.Blue,out unit);
                unitRoot = unit.gameObject;
                _modEntry.Logger.Log("创建完成!");
                //创建GUI
                var gui = new GameObject("UnitEditorGUI");
                gui.AddComponent<UnitEditorGUI>();
            }
        }
    }
    //相机旋转器
    public class CameraController : MonoBehaviour {
        Rigidbody rigid;
        float moveSpeed = 100;
        float rotateSpeed = 40;
        void Start() {
            rigid = GetComponent<Rigidbody>();
        }

        void Update() {
            var horizontalInput = Input.GetAxisRaw("Horizontal");
            var verticalInput = Input.GetAxisRaw("Vertical");
            var direction = (horizontalInput * transform.right + verticalInput * transform.forward).normalized;
            var targetPos = transform.position + direction * Time.deltaTime * moveSpeed;

            if (Input.GetKey(KeyCode.Q)) {
                transform.Rotate(Vector3.up, -rotateSpeed * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.E)) {
                transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.Space)) {
                targetPos.y += moveSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.LeftControl)) {
                targetPos.y -= moveSpeed * Time.deltaTime;
            }
            rigid.MovePosition(targetPos);
        }
    }
    public class UnitEditorGUI : MonoBehaviour
    {
        List<GameObject> weapons;

        int currentIndex = 0;

        void Start() {
            var database = LandfallUnitDatabase.GetDatabase();
            weapons = database.Weapons;
            currentIndex = 0;
        }
        void OnGUI() {
            
            GUILayout.BeginArea(new Rect(new Vector2(Screen.width - 600, 30), new Vector2(600, 50)));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("上一个武器")) {
                ChangeIndex(-1);
                ApplyWeapon();
            }
            GUILayout.Label(weapons[currentIndex].name);
            if (GUILayout.Button("下一个武器")) {
                ChangeIndex(1);
                ApplyWeapon();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void ChangeIndex(int changed) {
            currentIndex += changed;
            if (currentIndex < 0) currentIndex += weapons.Count;
            if (currentIndex >= weapons.Count) currentIndex -= weapons.Count;
        }
        void ApplyWeapon() {
            var unitRig = Main.unitRoot.GetComponent<UnitRig>();
            var prop = weapons[currentIndex].GetComponent<CharacterItem>();
            var propData = new PropItemData();
            SpawnProp(prop, propData);
        }

        void SpawnProp(CharacterItem prop, PropItemData propData) {
            Weapon component = prop.GetComponent<Weapon>();
            CharacterItem component2;
            if (component == null) {
                component2 = Main.unitRoot.GetComponent<UnitRig>().SpawnProp(prop, propData, Stitcher.TransformCatalog.RigType.Human, Team.Blue, null, true).GetComponent<CharacterItem>();
            } 
            else {
                //处理已存在的武器
                //if (this.m_hasWeapon) {
                //    return null;
                //}
                //this.m_hasWeapon = true;
                Quaternion rotation = Main.unitRoot.GetComponentInChildren<WeaponHandler>().transform.rotation;
                component2 = LandfallUnitDatabase.GetDatabase().m_unitEditorBlueprint.SetWeapon(Main.unitRoot.GetComponent<Unit>(), Team.Blue, prop.gameObject, propData, HoldingHandler.HandType.Right, rotation, new List<GameObject>()).GetComponent<WeaponItem>();
            }
        }
    }
}
