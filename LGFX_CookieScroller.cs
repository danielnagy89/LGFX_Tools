/// LGFX_Tools CookieScroller - Scrolls the X and Y of the Directional Light Cookie parameters
/// Direction, design and tweaks by Daniel Nagy, code executed by Claude.ai
/// You got this from https://github.com/danielnagy89/LGFX_Tools
/// Supports multi-select, edit "enabledButtonColor" values to style
/// tested on Unity6 URP, can't guarantee compatibility with previous versions, use at your own risk.


using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light))]
[RequireComponent(typeof(UniversalAdditionalLightData))]
public class LGFX_CookieScroller : MonoBehaviour
{
    [Header("Scrolls Directional Light's cookie via URP's lightCookieOffset")]
    [Space(10)]
    [Tooltip("Units/ second in X and Y. Negatives reverse direction.")]
    public Vector2 scrollSpeed = new Vector2(1f, 1f);

    private UniversalAdditionalLightData _lightData;

    void Start()
    {
        _lightData = GetComponent<UniversalAdditionalLightData>();
    }

    void Update()
    {
        _lightData.lightCookieOffset += scrollSpeed * Time.deltaTime;
    }
}
