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
        static UnityModManager.ModEntry _modEntry;
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
                _modEntry.Logger.Log("-创建UnitRoot");
                var unitRoot = new GameObject("UnitRoot");
                unitRoot.transform.position = Vector3.zero;
                unitRoot.transform.rotation = Quaternion.identity;
                //获取蓝图,然后调用Spawn
                _modEntry.Logger.Log("-创建默认单位");
                var unitDatabase = LandfallUnitDatabase.GetDatabase();
                unitDatabase.m_unitEditorBlueprint.Spawn(Vector3.zero, Quaternion.identity, Team.Blue);
                _modEntry.Logger.Log("创建完成!");

            }
        }
    }
}
