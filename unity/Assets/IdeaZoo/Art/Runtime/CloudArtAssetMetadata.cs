using UnityEngine;

namespace IdeaZoo.EditorTools
{
    [DisallowMultipleComponent]
    public sealed class CloudArtAssetMetadata : MonoBehaviour
    {
        public string AssetId;
        public string Category;
        public string ArtDirection = "Civic Surrealism";
        public int SchemaVersion = 1;
    }
}
