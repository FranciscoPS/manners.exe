using UnityEngine;

public interface ISynergyEffect
{
    void Activate(Transform player, SynergyData source);
    void Deactivate();
}
