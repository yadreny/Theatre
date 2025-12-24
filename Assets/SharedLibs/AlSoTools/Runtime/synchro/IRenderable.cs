using System;

namespace AlSo
{
    public interface IRenderable
    {
        string Label { get; }
        object getSelf();
    }

    public class EasyRenderer : IRenderable
    {
        private Func<string> titleReturner;
        private Func<object> dataReturner;
        public EasyRenderer(Func<string> titleReturner, Func<object> dataReturner) => (this.titleReturner, this.dataReturner) = (titleReturner, dataReturner);

        public object getSelf() => dataReturner();

        public string Label => titleReturner();
    }

    public class EasyTypedRenderer<T> : IRenderable
    {
        private Func<string> titleReturner;
        private Func<T> dataReturner;
        public EasyTypedRenderer(Func<string> titleReturner, Func<T> dataReturner)
        {
            this.titleReturner = titleReturner;
            this.dataReturner = dataReturner;
        }

        public object getSelf() => dataReturner();

        public T self => dataReturner();

        public string Label => titleReturner();
    }
}