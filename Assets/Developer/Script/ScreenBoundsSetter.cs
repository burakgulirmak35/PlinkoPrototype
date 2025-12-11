using UnityEngine;

namespace PlinkoPrototype
{
    public class ScreenBoundsSetter : MonoBehaviour
    {
        [Header("Walls")]
        public Transform leftWall;
        public Transform rightWall;
        public Transform bottomWall;

        [Header("Wall Settings")]
        public float wallThickness = 0.5f; // X scale'i (sen zaten 0.5 demiştin)

        private void Start()
        {
            FitWalls();
        }

        private void FitWalls()
        {
            Camera cam = Camera.main;

            // Ekranın 4 köşesini world space'e çevir
            Vector3 leftBottom = cam.ScreenToWorldPoint(new Vector3(0, 0, -cam.transform.position.z));
            Vector3 rightBottom = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, -cam.transform.position.z));
            Vector3 leftTop = cam.ScreenToWorldPoint(new Vector3(0, Screen.height, -cam.transform.position.z));

            float leftX = leftBottom.x;
            float rightX = rightBottom.x;
            float bottomY = leftBottom.y;

            // ✦ Sol Duvar
            // X konumu sol sınırda olmalı
            leftWall.position = new Vector3(leftX - wallThickness * 0.5f, 0, 0);
            leftWall.localScale = new Vector3(wallThickness, leftTop.y - bottomY, 1);

            // ✦ Sağ Duvar
            rightWall.position = new Vector3(rightX + wallThickness * 0.5f, 0, 0);
            rightWall.localScale = new Vector3(wallThickness, leftTop.y - bottomY, 1);

            // ✦ Alt Duvar
            bottomWall.position = new Vector3(0, bottomY - wallThickness * 0.5f, 0);
            bottomWall.localScale = new Vector3(rightX - leftX, wallThickness, 1);
        }
    }
}
