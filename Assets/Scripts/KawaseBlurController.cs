using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KawaseBlurController
{
    // 抓屏，并且接下来对屏幕逐帧做模糊操作
    // Note: 注意抓屏操作通常在帧尾，所以需要在模糊背景上层出现的内容，应当至少在下一帧再active
    static public void GrabScreenAndBlur() {

    }

    static public void StopBlur() {

    }
}


