using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance { get; private set; }
    private List<EventInstance> eventInstances = new List<EventInstance>();
    private List<StudioEventEmitter> eventEmitters = new List<StudioEventEmitter>();

    private EventInstance ambientEventInstance;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one AudioManager in scene.");
        }
        
        instance = this;
    }

    private void Start()
    {
        InitializeAmbientEventInstance(FmodEvents.instance.ambientWind);
        PLAYBACK_STATE state;
        ambientEventInstance.getPlaybackState(out state);
        Debug.Log(state);
    }

    public void PlayOneShot(EventReference sound, Vector3 worldPosition)
    {
        RuntimeManager.PlayOneShot(sound, worldPosition);
    }

    public StudioEventEmitter InitializeEventEmitters(EventReference sound, GameObject emitterGameObject)
    {
        StudioEventEmitter emitter = emitterGameObject.GetComponent<StudioEventEmitter>();
        emitter.EventReference = sound;
        eventEmitters.Add(emitter);
        return emitter;
    }

    public void InitializeAmbientEventInstance(EventReference eventReference)
    {
        ambientEventInstance = CreatEventInstance(eventReference);
        ambientEventInstance.start();
    }

    public void SetAmbientParameter(string parameterName, float value)
    {
        ambientEventInstance.setParameterByName(parameterName, value);
    }
    
    
    //EventReference 就像是事件在编辑器中的地址或指针；而 EventInstance 是这个事件在游戏运行中的实际对象，可供你操作和管理声音的生命周期。
    /*因此，RuntimeManager.CreateInstance(eventRef) 的主要作用是：
    “基于传入的 FMOD 事件引用，生成一个可操控的事件实例对象，然后返回给你供后续操作。”
    后面就可以用这个返回的 EventInstance 来调用 start()、stop()、setParameterByName() 等各种方法，从而实际控制声音的行为。*/

    public EventInstance CreatEventInstance(EventReference eventRef)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventRef);
        eventInstances.Add(eventInstance);
        return eventInstance;
    }

    public void CleanUp()
    {
        foreach (EventInstance eventInstance in eventInstances)
        {
            eventInstance.stop(STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }

        foreach (var emitter in eventEmitters)
        {
            emitter.Stop();
        }
    }

    public void OnDestroy()
    {
        CleanUp();
    }
    
    
    
}
