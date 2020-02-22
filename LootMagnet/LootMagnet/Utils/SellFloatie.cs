using System.Collections;
using TMPro;
using UnityEngine;

namespace LootMagnet
{
    public class FloatieBehaviour : MonoBehaviour
    {

        private float speed = 40f;

        void Update() {
 
            gameObject.transform.Translate(Vector3.up * (Time.deltaTime * speed));
            gameObject.transform.localScale += new Vector3(0.001f, 0.001f, 0.001f);
        }
    }

    public class FadeText : MonoBehaviour
    {
        private static float fadeSpeed = 0.6f;
        private TextMeshProUGUI text;

        void Awake() {

            text = gameObject.GetComponentInChildren<TextMeshProUGUI>(true);
            StartCoroutine(FadeOutText());

            IEnumerator FadeOutText() {
                text.color = new Color(text.color.r, text.color.g, text.color.b, 1);
                while (text.color.a > 0)
                {
                    text.color = new Color(
                        text.color.r, text.color.g, text.color.b, text.color.a - Time.deltaTime * fadeSpeed);
                    yield return null;
                }

                Destroy(gameObject);
            }
        }
    }
}
