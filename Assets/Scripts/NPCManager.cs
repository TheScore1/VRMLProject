using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SpeechAnalyzer;

public class NPCManager : MonoBehaviour
{
    [System.Serializable]
    public class NPCReaction
    {
        public float minConfidence;
        public float maxConfidence;
        public string animationTrigger;
    }

    public List<NPCReaction> reactions = new List<NPCReaction>();
    public Animator[] npcAnimators;

    public void TriggerNPCReactions(SpeechAnalysis analysis)
    {
        foreach (NPCReaction reaction in reactions)
        {
            if (analysis.confidenceLevel >= reaction.minConfidence &&
                analysis.confidenceLevel <= reaction.maxConfidence)
            {
                TriggerAnimationForAllNPCs(reaction.animationTrigger);
                break;
            }
        }

        // Дополнительные реакции на основе конкретных метрик
        ReactToSpecificMetrics(analysis);
    }

    private void TriggerAnimationForAllNPCs(string triggerName)
    {
        foreach (Animator animator in npcAnimators)
        {
            if (animator != null)
            {
                animator.SetTrigger(triggerName);
            }
        }
    }

    private void ReactToSpecificMetrics(SpeechAnalysis analysis)
    {
        // Реакции на конкретные проблемы
        if (analysis.fillerWordsCount > 5)
        {
            TriggerAnimationForAllNPCs("confused");
        }

        if (analysis.speakingRate > 180)
        {
            TriggerAnimationForAllNPCs("overwhelmed");
        }

        if (analysis.clarityScore > 0.7f && analysis.confidenceLevel > 0.8f)
        {
            TriggerAnimationForAllNPCs("applaud");
        }
    }
}