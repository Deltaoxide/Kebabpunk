using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]


// ------- This Script Resizes the sprite on X Axis to be used when canvases stretch mode matches height.
public class SpriteResizer : MonoBehaviour
{
    [Header("Anchors")]
    [Header("Event Trigger")]
    [SerializeField] ScreenAspectChangeListener screenAspectChangeListener;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider2D;
    private Vector2 originalsize;
    private Vector3 originalpos;
    

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        spriteRenderer.drawMode = SpriteDrawMode.Tiled;
        
        originalsize = spriteRenderer.size;
        originalpos = transform.localPosition;
    }
    void OnEnable()
    {
        screenAspectChangeListener.OnScreenSizeChanged += ResizeNow;
    }
    void OnDisable()
    {
        screenAspectChangeListener.OnScreenSizeChanged -= ResizeNow;
    }

    private void ResizeNow(float w, float h)
    {
        spriteRenderer.size = new Vector2(w,originalsize.y);
        transform.position = new Vector3(originalpos.x+(1920-w)/2,originalpos.y,originalpos.z);
    }

}
