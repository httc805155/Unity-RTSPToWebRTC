using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Unity.WebRTC.Samples;

public class WebRTCHelper : MonoBehaviour
{
    public string HOST_URL { get { return host.EndsWith("/") ? host : host + "/"; } }

    [SerializeField]
    private RawImage image;

    [Header("RTSP2WEB Settings")]
    [SerializeField]
    private string host = "http://127.0.0.1:8083";
    [SerializeField]
    private string streamId = string.Empty;
    [SerializeField]
    private string channelId = string.Empty;

    private RTCPeerConnection peer;
    private RTCDataChannel dataChannel;
    private bool videoUpdateStarted;

    private void Start()
    {
        Call();
    }

    public void Call()
    {
        var configuration = GetSelectedSdpSemantics();

        peer = new RTCPeerConnection(ref configuration);
        peer.OnNegotiationNeeded += handleNegotiationNeeded;
        peer.OnTrack += handleOnTrack;

        var transciever = peer.AddTransceiver(TrackKind.Video);
        transciever.Direction = RTCRtpTransceiverDirection.RecvOnly;


        //dataChannel = peer.CreateDataChannel("sendChannel");
        //dataChannel.OnClose = () => { Debug.Log("sendChannel has closed"); };
        //dataChannel.OnOpen = () =>
        //{
        //    Debug.Log("sendChannel has opend");
        //    dataChannel.Send("ping");
        //    StartCoroutine(PingingCoroutine());
        //};
        //dataChannel.OnMessage = e => { Debug.Log(Convert.ToBase64String(e)); };

        if (!videoUpdateStarted)
        {
            StartCoroutine(WebRTC.Update());
            videoUpdateStarted = true;
        }
    }


    //private IEnumerator PingingCoroutine()
    //{
    //    dataChannel.Send("ping");
    //    yield return new WaitForSeconds(1.0f);
    //}


    private static RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };

        return config;
    }

    private void handleNegotiationNeeded()
    {
        StartCoroutine(PeerNegotiationNeeded(peer));
    }

    private void handleOnTrack(RTCTrackEvent e)
    {
        if (e.Track is VideoStreamTrack track)
        {
            track.OnVideoReceived += tex =>
            {
                image.texture = tex;
            };
        }
    }

    IEnumerator PeerNegotiationNeeded(RTCPeerConnection pc)
    {
        //Debug.Log($"{GetName(pc)} createOffer start");
        var op = pc.CreateOffer();
        yield return op;

        if (!op.IsError)
        {
            if (pc.SignalingState != RTCSignalingState.Stable)
            {
                //Debug.LogError($"{GetName(pc)} signaling state is not stable.");
                yield break;
            }

            yield return StartCoroutine(OnCreateOfferSuccess(pc, op.Desc));
            // Post to server;
            yield return PostToServer(op.Desc);
        }
        else
        {
            OnCreateSessionDescriptionError(op.Error);
        }
    }

    private IEnumerator PostToServer(RTCSessionDescription desc)
    {
        if (string.IsNullOrEmpty(HOST_URL) || string.IsNullOrEmpty(streamId) || string.IsNullOrEmpty(channelId))
            yield break;

        byte[] bytesToEncode = Encoding.UTF8.GetBytes(desc.sdp);
        var encodedText = Convert.ToBase64String(bytesToEncode);

        var url = $"{HOST_URL}stream/{streamId}/channel/{channelId}/webrtc";
        var formData = new WWWForm();
        formData.AddField("data", encodedText);

        var www = UnityWebRequest.Post(url, formData);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            byte[] decodeBytes = Convert.FromBase64String(www.downloadHandler.text);
            string decodedText = Encoding.UTF8.GetString(decodeBytes);

            var remoteSDP = new RTCSessionDescription();
            remoteSDP.type = RTCSdpType.Answer;
            remoteSDP.sdp = decodedText;
            peer.SetRemoteDescription(ref remoteSDP);
        }
    }

    private IEnumerator OnCreateOfferSuccess(RTCPeerConnection pc, RTCSessionDescription desc)
    {
        //Debug.Log($"Offer from {GetName(pc)}\n{desc.sdp}");
        //Debug.Log($"{GetName(pc)} setLocalDescription start");
        var op = pc.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            OnSetLocalSuccess(pc);
        }
        else
        {
            var error = op.Error;
            OnSetSessionDescriptionError(ref error);
        }
    }

    static void OnSetSessionDescriptionError(ref RTCError error)
    {
        Debug.LogError($"Error Detail Type: {error.message}");
    }

    private void OnSetLocalSuccess(RTCPeerConnection pc)
    {
        Debug.Log("SetLocalDescription complete");
    }

    private static void OnCreateSessionDescriptionError(RTCError error)
    {
        Debug.LogError($"Error Detail Type: {error.message}");
    }
}
