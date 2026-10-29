using UnityEngine;

public static class UIEditorHierarchy
{
    public static Transform Find(Transform root, string path)
    {
        if (root == null) return null;
        if (string.IsNullOrEmpty(path)) return root;

        Transform current = root;
        foreach (string segment in path.Split('/'))
        {
            if (string.IsNullOrEmpty(segment)) continue;
            Transform next = current.Find(segment);
            if (next == null)
            {
                Transform frame = current.Find("ContentFrame");
                if (frame != null) next = frame.Find(segment);
            }
            if (next == null) return null;
            current = next;
        }
        return current;
    }
}
