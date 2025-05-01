using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.IO;

public class FishManager : MonoBehaviour
{
    public Material wobble;
    public GameObject spawnbubbles;
    public string listUrl = "http://127.0.0.1:3000/list";
    private HashSet<string> spawnedFish = new HashSet<string>();
    public float refreshInterval = 5f;
    public float FishSize = 50f;

    private void Start()
    {
        InvokeRepeating(nameof(CheckForNewFish), 0f, refreshInterval);
    }

    void CheckForNewFish()
    {
        StartCoroutine(FetchFishList());
    }

    IEnumerator FetchFishList()
    {    
        UnityWebRequest request = UnityWebRequest.Get(listUrl);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            List<string> urls = JsonConvert.DeserializeObject<List<string>>(request.downloadHandler.text);

            foreach (var url in urls)
            {
                if (!spawnedFish.Contains(url))
                {
                    spawnedFish.Add(url);
                    StartCoroutine(SpawnFish(url));
                }
            }
        }
        else
        {
            string path = Application.dataPath + "/Log.txt";
            //Create File if it doesn't exist
            if (!File.Exists(path)) {
            File.WriteAllText(path, "Login log \n\n");
            }
            //Content of the file
            string content = request.error.ToString();
            //Add some to text to it
            File.AppendAllText(path, content);
        }
    }

    IEnumerator SpawnFish(string imageUrl)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            GameObject fish = new GameObject("Fish");
            var renderer = fish.AddComponent<SpriteRenderer>();
            renderer.material = wobble;
            renderer.sprite = sprite;
        
            
            FishSize = 200f;
            Vector3 scale = new Vector3((1f/texture.width) * FishSize, (1f/texture.height) * FishSize, 1);
            fish.transform.localScale = scale;

            fish.transform.position = new Vector3(Random.Range(-8f, 8f), Random.Range(-4f, 4f), 0);
            Destroy(Instantiate(spawnbubbles, fish.transform.position, Quaternion.identity), 10f);


            fish.AddComponent<BoidFish>();

        }
        else
        {
            Debug.LogError(request.error);
        }
    }
}