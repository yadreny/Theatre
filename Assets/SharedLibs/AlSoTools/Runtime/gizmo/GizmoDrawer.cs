using UnityEngine;

namespace AlSo
{
    public class PointShower
    { 
        public Vector3 Point { get; }
        public Color Color { get; }
        private GameObject Representation { get; }
        public PointShower(Vector3 point, Color color)
        {
            this.Point = point;
            this.Color = color;
            Representation = new GameObject();
            Representation.AddComponent<PointShowerBehaviour>().Source = this;
        }

        public void Destroy() => GameObject.Destroy(Representation);
    }

    public class PointShowerBehaviour : MonoBehaviour
    {
        public PointShower Source { get; set; }

        private void OnDrawGizmos()
        {
            Gizmos.color = Source.Color;
            Gizmos.DrawSphere(Source.Point, 0.1f);
            //Gizmos.DrawLine(Vector3.zero, Source.Point);
        }
    }

    public class RayShower
    {
        public Vector3 Start { get; }
        public Vector3 Direction { get; }
        public float Len { get; }

        private GameObject Representation { get; }

        public RayShower(Vector3 start, Vector3 finsih)
        {
            this.Start = start;
            this.Direction = finsih - start;
            this.Len = (finsih - start).magnitude;
            Representation = new GameObject();
            Representation.AddComponent<RayShowerBehaviour>().shower = this;
        }

        public RayShower(Vector3 start, Vector3 direction, float len)
        {
            this.Start = start;
            this.Direction = direction;
            this.Len = len;
            Representation = new GameObject();
            Representation.AddComponent<RayShowerBehaviour>().shower = this;
        }

        public void Destroy()=> GameObject.Destroy(Representation);
    }

    public class RayShowerBehaviour : MonoBehaviour
    {
        public RayShower shower;
        private void OnDrawGizmos()
        {
            //if (shower == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(shower.Start, shower.Start + shower.Direction.normalized * shower.Len);
        }
    }
}