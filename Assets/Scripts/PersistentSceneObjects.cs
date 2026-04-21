using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentSceneObjects : MonoBehaviour
{
    private static PersistentSceneObjects instance;
    private static Pose? lastPose;

    [SerializeField] private List<GameObject> persistentObjects = new();
    [SerializeField] private Transform poseTarget;

    private Dictionary<string, GameObject> persistentByName;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        persistentByName = new Dictionary<string, GameObject>();
        foreach (var obj in persistentObjects)
        {
            if (!obj)
            {
                continue;
            }

            DontDestroyOnLoad(obj);
            persistentByName[obj.name] = obj;
        }

        if (!poseTarget)
        {
            poseTarget = transform;
        }

        if (lastPose.HasValue)
        {
            ApplyPose(lastPose.Value);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void LateUpdate()
    {
        if (poseTarget)
        {
            lastPose = new Pose(poseTarget.position, poseTarget.rotation);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (persistentByName.TryGetValue(root.name, out var keep) && root != keep)
            {
                Destroy(root);
            }
        }

        if (lastPose.HasValue)
        {
            ApplyPose(lastPose.Value);
        }
    }

    private void ApplyPose(Pose pose)
    {
        if (poseTarget)
        {
            poseTarget.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }
}
