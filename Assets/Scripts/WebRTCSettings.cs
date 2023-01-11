using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;

public static class WebRTCSettings
{
    public const int DefaultStreamWidth = 1280;
    public const int DefaultStreamHeight = 720;

    private static Vector2Int s_StreamSize = new Vector2Int(DefaultStreamWidth, DefaultStreamHeight);
    private static RTCRtpCodecCapability s_useVideoCodec = null;

    public static Vector2Int StreamSize
    {
        get { return s_StreamSize; }
        set { s_StreamSize = value; }
    }

    public static RTCRtpCodecCapability UseVideoCodec
    {
        get { return s_useVideoCodec; }
        set { s_useVideoCodec = value; }
    }
}