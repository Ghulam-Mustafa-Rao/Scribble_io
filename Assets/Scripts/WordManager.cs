using UnityEngine;
using System.Collections.Generic;

public class WordManager : MonoBehaviour
{
    public List<string> words = new List<string> { "Apple", "Dog", "Car", "House", "Tree" };

    public string GetRandomWord()
    {
        int index = Random.Range(0, words.Count);
        return words[index];
    }
}
