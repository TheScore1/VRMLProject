using System.Collections.Generic;
using UnityEngine;

public class SmoothIdleAnimationPicker : MonoBehaviour
{
    [System.Serializable]
    private class ChildAnimatorData
    {
        public GameObject childObject;
        public Animator animator;
        public float timer;
        public int currentIdleIndex;
        public bool isReacting;
        public bool pendingAnimationChange;
        public float lastNormalizedTime;
    }

    [Header("Animation Settings")]
    [SerializeField] private string idleParameterName = "IdleIndex";
    [SerializeField] private string reactingParameterName = "IsReacting";
    [SerializeField] private int minIdleIndex = 0;
    [SerializeField] private int maxIdleIndex = 2;

    [Header("Timer Settings")]
    [SerializeField] private float randomizeInterval = 3f;
    [SerializeField] private bool randomizeOnStart = true;
    [SerializeField] private bool useRandomInterval = false;
    [SerializeField] private float minInterval = 2f;
    [SerializeField] private float maxInterval = 5f;

    private List<ChildAnimatorData> childrenData = new List<ChildAnimatorData>();

    void Start()
    {
        InitializeChildren();

        if (randomizeOnStart)
        {
            RandomizeAllIdleAnimations();
        }

        InitializeTimers();
    }

    void Update()
    {
        UpdateChildrenTimers();
        CheckForAnimationCycleCompletion();
    }

    void InitializeChildren()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            Animator animator = child.GetComponent<Animator>();

            if (animator != null)
            {
                ChildAnimatorData data = new ChildAnimatorData
                {
                    childObject = child,
                    animator = animator,
                    timer = 0f,
                    currentIdleIndex = animator.GetInteger(idleParameterName),
                    isReacting = animator.GetBool(reactingParameterName),
                    pendingAnimationChange = false,
                    lastNormalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime
                };

                childrenData.Add(data);

                StartCoroutine(MonitorReactionState(data));
            }
        }
    }

    void InitializeTimers()
    {
        for (int i = 0; i < childrenData.Count; i++)
        {
            if (useRandomInterval)
            {
                childrenData[i].timer = Random.Range(0f, GetRandomInterval());
            }
        }
    }

    void UpdateChildrenTimers()
    {
        for (int i = 0; i < childrenData.Count; i++)
        {
            ChildAnimatorData data = childrenData[i];

            if (data.isReacting || data.pendingAnimationChange)
            {
                continue;
            }

            data.timer += Time.deltaTime;

            float interval = useRandomInterval ? GetRandomInterval() : randomizeInterval;

            if (data.timer >= interval)
            {
                data.pendingAnimationChange = true;
                data.timer = 0f;
            }
        }
    }

    void CheckForAnimationCycleCompletion()
    {
        for (int i = 0; i < childrenData.Count; i++)
        {
            ChildAnimatorData data = childrenData[i];

            if (!data.pendingAnimationChange || data.isReacting)
            {
                data.lastNormalizedTime = data.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                continue;
            }

            float currentNormalizedTime = data.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

            if (Mathf.Floor(currentNormalizedTime) > Mathf.Floor(data.lastNormalizedTime))
            {
                RandomizeIdleAnimation(data);
                data.pendingAnimationChange = false;
            }

            data.lastNormalizedTime = currentNormalizedTime;
        }
    }

    void RandomizeIdleAnimation(ChildAnimatorData data)
    {
        if (data.animator == null || data.isReacting) return;

        int newIndex = GetRandomIdleIndex(data.currentIdleIndex);
        data.animator.SetInteger(idleParameterName, newIndex);
        data.currentIdleIndex = newIndex;
    }

    void RandomizeAllIdleAnimations()
    {
        foreach (var data in childrenData)
        {
            if (!data.isReacting)
            {
                RandomizeIdleAnimation(data);
            }
        }
    }

    int GetRandomIdleIndex(int excludeIndex = -1)
    {
        if (maxIdleIndex <= minIdleIndex)
            return minIdleIndex;

        int newIndex;

        if (excludeIndex >= minIdleIndex && excludeIndex <= maxIdleIndex &&
            (maxIdleIndex - minIdleIndex) > 0)
        {
            do
            {
                newIndex = Random.Range(minIdleIndex, maxIdleIndex + 1);
            }
            while (newIndex == excludeIndex);
        }
        else
        {
            newIndex = Random.Range(minIdleIndex, maxIdleIndex + 1);
        }

        return newIndex;
    }

    float GetRandomInterval()
    {
        return Random.Range(minInterval, maxInterval);
    }

    System.Collections.IEnumerator MonitorReactionState(ChildAnimatorData data)
    {
        while (true)
        {
            bool currentReaction = data.animator.GetBool(reactingParameterName);

            if (currentReaction != data.isReacting)
            {
                data.isReacting = currentReaction;

                if (currentReaction)
                {
                    data.animator.SetInteger(idleParameterName, 0);
                    data.currentIdleIndex = 0;
                    data.timer = 0f;
                    data.pendingAnimationChange = false;
                }
                else
                {
                    RandomizeIdleAnimation(data);
                    data.pendingAnimationChange = false;
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }
}