using UnityEngine;

public sealed class NewsBulletinSchedule
{
    private readonly int[] nextHeadline = new int[NewsBroadcastConfig.StoryCount];
    private float time;
    private float visibleUntil = float.NegativeInfinity;
    private float quietUntil = float.NegativeInfinity;
    private int currentPriority = -1;

    public bool IsShowing => time < visibleUntil;

    public void Reset()
    {
        time = 0f;
        visibleUntil = float.NegativeInfinity;
        quietUntil = float.NegativeInfinity;
        currentPriority = -1;
        for (int i = 0; i < nextHeadline.Length; i++) nextHeadline[i] = 0;
    }

    public bool Advance(float deltaTime)
    {
        bool wasShowing = IsShowing;
        time += Mathf.Max(0f, deltaTime);
        return wasShowing && !IsShowing;
    }

    public bool TryStart(int priority, float duration, float cooldown)
    {
        bool busy = time < visibleUntil || time < quietUntil;
        if (busy && priority <= currentPriority) return false;

        visibleUntil = time + Mathf.Max(0f, duration);
        quietUntil = visibleUntil + Mathf.Max(0f, cooldown);
        currentPriority = priority;
        return true;
    }

    public int NextHeadlineIndex(NewsStory story, int poolSize)
    {
        if (poolSize <= 0) return -1;

        int slot = (int)story;
        int index = nextHeadline[slot] % poolSize;
        nextHeadline[slot] = index + 1;
        return index;
    }
}
