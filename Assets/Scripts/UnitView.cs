using UnityEngine;

public class UnitView : MonoBehaviour {
    public NetUnit state;

    private SpriteRenderer spriteRenderer; 
    [Header("Unique Unit Mask")]
    public Texture2D unitMaskTexture; 
    private MaterialPropertyBlock propBlock;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip deathSound;

    public void Initialize(NetUnit serverData) {
        if (audioSource == null) {
            audioSource = GetComponent<AudioSource>();
        }
        this.state = serverData;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null) {
            if (propBlock == null)
                propBlock = new MaterialPropertyBlock();

            spriteRenderer.GetPropertyBlock(propBlock);

            propBlock.SetColor("_PlayerColor", playerColor(serverData.playerIdx));

            if (unitMaskTexture != null)
                propBlock.SetTexture("_Mask", unitMaskTexture);

            spriteRenderer.SetPropertyBlock(propBlock);
        }

        if (audioSource == null) {
            audioSource = GetComponent<AudioSource>();
        }

        UpdateVisuals();
    }

    public void UpdateData(NetUnit newData) {
        this.state = newData;
        UpdateVisuals();
    }

    private void UpdateVisuals() {
        // healthBar.SetValue(state.curHealth);
        // some unit rerender
        Debug.Log($"Unit {state.unitId} updated. HP: {state.curHealth}");
    }

    public void Select() {
        transform.localScale = new Vector3(2.1f, 2.1f, 1);
        transform.position += new Vector3(0, 0.2f, 0);
    }

    public void Deselect() {
        transform.localScale = new Vector3(2, 2, 1);
        transform.position -= new Vector3(0, 0.2f, 0);
    }

    public void PlayHitSound() {
        if (audioSource != null && hitSound != null) {
            audioSource.PlayOneShot(hitSound);
        }
    }

    public void Kill() {
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);
        Destroy(gameObject);
    }

    private Color playerColor(int playerId) {
        switch(playerId) {
            case 0: return Color.cyan;
            case 1: return Color.red;
            case 2: return Color.green;
            case 3: return Color.blue;
            case 4: return Color.yellow;
            case 5: return Color.white;
            default: return Color.magenta;
        }
    }
}