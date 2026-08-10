namespace ProjectDataLib
{
    public class PO
    {
        private int X_;

        public int X
        {
            set { X_ = value; }
            get { return X_; }
        }

        private int Y_;

        public int Y
        {
            set { Y_ = value; }
            get { return Y_; }
        }

        private int BlockNum_;

        public int BlockNum
        {
            get { return BlockNum_; }
            set { BlockNum_ = value; }
        }

        public PO()
        {
        }

        public PO(int x, int y)
        {
            this.X_ = x;
            this.Y_ = y;
        }
    }
}