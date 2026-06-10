using UnityEngine;

public class NarrationStarter : MonoBehaviour
{
    public void TestLog()
    {
        Debug.Log("[NarrationStarter] Botón funcionando correctamente.");
    }

    public void OnStartButtonPressed()
    {
        var narrator = FindObjectOfType<NarratorNetworkController>();
        if (narrator != null)
            narrator.StartNarration();
        else
            Debug.LogError("[NarrationStarter] No se encontró NarratorNetworkController en la escena.");
    }
}
