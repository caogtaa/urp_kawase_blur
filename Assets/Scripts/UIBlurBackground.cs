using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UIBlurBackground : MonoBehaviour
{
    [SerializeField]
    Image _image;

    [SerializeField]
    Material _blurMaterial;

    // [SerializeField]
    // int _iteratorNum = 40;

    [SerializeField]
    float _iteratorSpeed = 25;

    // [SerializeField]
    // bool hideBackgroundScene = true;

    RenderTexture _rt1;
    RenderTexture _rt2;
    Material _m1;
    Material _m2;
    int _iteratorCount = 0;
    float _elapse = 1;

    Camera m_targetCamera = null;

    // GameObject sceneRoot = null;

    private void Awake()
    {
        if(_image == null) {
            _image = GetComponent<Image>();
        }
        _elapse = 1f;
        // _image.CrossFadeAlpha(0, 0, true);
        _m1 = new Material(_image.material);
        _m2 = new Material(_image.material);
    }

    private void Start()
    {
        // _rt1 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, GraphicsFormat.R16G16B16A16_SFloat);
        // _rt2 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, GraphicsFormat.R16G16B16A16_SFloat);
        // _rt1 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, GraphicsFormat.B10G11R11_UFloatPack32);
        // _rt2 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, GraphicsFormat.B10G11R11_UFloatPack32);

        _rt1 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.DefaultHDR);
        _rt2 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.DefaultHDR);

        _m1.mainTexture = _rt1;
        _m2.mainTexture = _rt2;

        if(m_targetCamera == null)
        {
            m_targetCamera = Camera.main;   // GeCameraControllerScroll.CurMainCamera;
        }

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += onEndCameraRendering;
        // RectTransform rectTrans = transform as RectTransform;
        // rectTrans.position = Vector3.zero;

        // var rootTrans = UIControllerManager.Inst.UI2DCanvas.GetComponent<RectTransform>();
        // var rect = rootTrans.rect;
        // var now = new Vector2(rect.width, rect.height);

        // RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTrans, now, null, out var realSize);

        // rectTrans.sizeDelta = realSize;

        // sceneRoot = GameObject.Find("/SceneRoot");
    }

    public void ChangeTargetCamera(Camera camera)
    {
        _iteratorCount = 0;
        _elapse = 1;
        m_targetCamera = camera;
    }

    public void ResetCamera()
    {
        // m_targetCamera = GeCameraControllerScroll.CurMainCamera;
        m_targetCamera = Camera.main;
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == m_targetCamera)
        {
            if (_iteratorCount == 0)
            {
                // var ev = G.UIEventsConfig.TownSceneRootActive;
                // ev.Active = true;
                // G.EventSystem.Handle(ev);
            }
        }
    }

    void onEndCameraRendering(ScriptableRenderContext context,Camera camera)
    {   
        if (camera == m_targetCamera) {
            _elapse += _iteratorSpeed * UnityEngine.Time.deltaTime;
            if (_elapse < 1) return;

            _elapse = 0;

            var cmd = new CommandBuffer();
            if (_iteratorCount == 0)
            {
                _image.material = _m1;
                _blurMaterial.SetFloat("_FlipWhenUVStartsAtTop", 1);
                //NOTE: 避免热更新 DLL 产生 TypeLoadException
                //  由于 AOT Unity Engine DLL 模块被裁剪，导致无法找到 ScreenCapture 类型
                //  此时需要重新构建 apk
                var typ = Type.GetType("UnityEngine.ScreenCapture,UnityEngine.ScreenCaptureModule");
                var method = typ?.GetMethod("CaptureScreenshotIntoRenderTexture");
                if (method != null) {
                    method.Invoke(typ, new object[] { _rt1 });
                } else {
                    // if (m_targetCamera == UIControllerManager.Inst.UICamera) {
                    //     m_targetCamera = GeCameraControllerScroll.CurMainCamera;
                    //     return;
                    // }
                    Debug.LogError("不要进");
                    cmd.Blit(BuiltinRenderTextureType.CurrentActive, _rt1, _blurMaterial);
                } 

                //if (hideBackgroundScene)
                //{
                //    var ev = G.UIEventsConfig.TownSceneRootActive;
                //    ev.Active = false;
                //    G.EventSystem.Handle(ev);
                //}
            }
            else if (_iteratorCount % 2 == 1)
            {
                _image.material = _m2;
                cmd.Blit(_rt1, _rt2, _blurMaterial);
            }
            else
            {
                _image.material = _m1;
                _blurMaterial.SetFloat("_FlipWhenUVStartsAtTop", 0);
                cmd.Blit(_rt2, _rt1, _blurMaterial);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();
            context.Submit();
            _iteratorCount++;
            if (_iteratorCount == 1)
            {
                // _image.CrossFadeAlpha(1, 0.3f, true);
            }
        }
    }

    private void OnDestroy()
    {
        RenderTexture.ReleaseTemporary(_rt1);
        RenderTexture.ReleaseTemporary(_rt2);
        Destroy(_m1);
        Destroy(_m2);
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= onEndCameraRendering;

        //if(hideBackgroundScene)
        //{
        //    var ev = G.UIEventsConfig.TownSceneRootActive;
        //    ev.Active = true;
        //    G.EventSystem.Handle(ev);
        //}
    }
}
