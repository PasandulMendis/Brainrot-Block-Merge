using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(SpriteRenderer))]
public class LaserEffect : MonoBehaviour
{
    private bool isRainbow;
    private bool isPulsing;
    private float scrollSpeedY;
    private string textPattern;
    private float laserWidth;
    private bool useTiling;

    private SpriteRenderer sr;
    private Color baseColor;
    private Material matInstance;

    private TextMeshProUGUI tmpText;
    private float textLineHeight = 1f;

    public void Setup(Sprite sprite, Color color, float width, bool tiled, bool rainbow, bool pulse, float scroll, string textPat, TMP_FontAsset customFont = null)
    {
        sr = GetComponent<SpriteRenderer>();

        baseColor = color;
        laserWidth = width;
        useTiling = tiled;
        isRainbow = rainbow;
        isPulsing = pulse;
        scrollSpeedY = scroll;
        textPattern = textPat;

        sr.color = baseColor;

        foreach (Transform child in transform)
        {
            if (child.name == "LaserCanvas") Destroy(child.gameObject);
        }
        tmpText = null;

        if (!string.IsNullOrEmpty(textPattern))
        {
            sr.enabled = false;

            GameObject canvasObj = new GameObject("LaserCanvas");
            canvasObj.transform.SetParent(this.transform);
            canvasObj.transform.localPosition = Vector3.zero;
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = Vector3.one;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.pivot = new Vector2(0.5f, 0.5f);

            canvasObj.AddComponent<RectMask2D>();

            GameObject txtObj = new GameObject("LaserText");
            txtObj.transform.SetParent(canvasObj.transform);
            txtObj.transform.localPosition = Vector3.zero;
            txtObj.transform.localRotation = Quaternion.identity;
            txtObj.transform.localScale = Vector3.one;

            tmpText = txtObj.AddComponent<TextMeshProUGUI>();
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = baseColor;

            if (customFont != null)
            {
                tmpText.font = customFont;
            }

            tmpText.fontSize = laserWidth * 0.8f;
            tmpText.enableWordWrapping = false;

            tmpText.text = "A";
            tmpText.ForceMeshUpdate();
            float h1 = tmpText.preferredHeight;
            tmpText.text = "A\nA";
            tmpText.ForceMeshUpdate();
            float h2 = tmpText.preferredHeight;
            textLineHeight = h2 - h1;

            string fullText = "";
            for (int i = 0; i < 60; i++) fullText += textPattern + "\n";
            tmpText.text = fullText;

            tmpText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            sr.enabled = true;
            sr.sprite = sprite;

            if (useTiling)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                if (scrollSpeedY != 0) matInstance = sr.material;
            }
            else
            {
                sr.drawMode = SpriteDrawMode.Simple;
            }
        }
    }

    public void SetDistance(float distance)
    {
        if (!string.IsNullOrEmpty(textPattern) && tmpText != null)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRT = canvas.GetComponent<RectTransform>();
                canvasRT.sizeDelta = new Vector2(laserWidth, distance);

                tmpText.rectTransform.sizeDelta = new Vector2(laserWidth, distance + 20f);
            }
            transform.localScale = Vector3.one;
        }
        else if (useTiling && sr != null)
        {
            sr.size = new Vector2(laserWidth, distance);
            transform.localScale = Vector3.one;
        }
        else
        {
            transform.localScale = new Vector3(laserWidth, distance, 1);
        }
    }

    void Update()
    {
        Color currentColor = baseColor;

        if (isRainbow)
        {
            currentColor = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.5f, 1f), 1f, 1f);
            currentColor.a = baseColor.a;
        }
        if (isPulsing)
        {
            float alphaPulse = (baseColor.a * 0.3f) + Mathf.PingPong(Time.time * 2f, baseColor.a * 0.7f);
            currentColor.a = alphaPulse;
        }

        if (tmpText != null) tmpText.color = currentColor;
        if (sr != null) sr.color = currentColor;

        if (scrollSpeedY != 0)
        {
            if (tmpText != null)
            {
                Vector3 pos = tmpText.rectTransform.localPosition;
                pos.y += scrollSpeedY * Time.deltaTime;

                if (pos.y < -textLineHeight) pos.y += textLineHeight;
                else if (pos.y > textLineHeight) pos.y -= textLineHeight;

                tmpText.rectTransform.localPosition = pos;
            }
            else if (matInstance != null && useTiling)
            {
                matInstance.mainTextureOffset += new Vector2(0, scrollSpeedY * Time.deltaTime);
            }
        }
    }
}