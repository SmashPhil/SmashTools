﻿using System.Collections.Generic;

namespace SmashTools
{
    public class RotatingList<T> : List<T>
    {
        private int currentIndex;

        public T Next
        {
            get
            {
                currentIndex++;
                if (currentIndex >= Count)
                {
                    currentIndex = 0;
                }    
                return this[currentIndex];
            }
        }

        public T Previous
        {
            get
            {
                currentIndex--;
                if (currentIndex < 0)
                {
                    currentIndex = Count - 1;
                }    
                return this[currentIndex];
            }
        }

        public T Current
        {
            get
            {
                return this[currentIndex];
            }
        }

        public void PostItemRemove()
        {
            if (Count == 0)
            {
                currentIndex = 0;
            }
            else
            {
                currentIndex %= Count;
            }
        }

        public void SetIndex(int index)
        {
            currentIndex = index % Count;
        }
    }
}
