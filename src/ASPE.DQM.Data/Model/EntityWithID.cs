using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ASPE.DQM.Model
{
    public abstract class EntityWithID
    {
        public EntityWithID()
        {
            ID = NewGuid();
        }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid ID { get; set; }

        [Timestamp]
        public byte[] Timestamp { get; set; }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is EntityWithID)
            {
                return ((EntityWithID)obj).ID == this.ID;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            if (obj == null || !(obj is EntityWithID))
                return -1;

            var ob = obj as EntityWithID;
            return this.ID.CompareTo(ob.ID);
        }

        public static implicit operator Guid(EntityWithID o)
        {
            return o.ID;
        }


        /// <summary>
        /// Gets a new sequential GUID that can be stored in a primary key
        /// </summary>
        /// <returns></returns>
        public static Guid NewGuid()
        {
            byte[] guidArray = System.Guid.NewGuid().ToByteArray();

            DateTime baseDate = new DateTime(1900, 1, 1);
            DateTime now = DateTime.Now;

            // Get the days and milliseconds which will be used to build the byte string 
            TimeSpan days = new TimeSpan(now.Ticks - baseDate.Ticks);
            TimeSpan msecs = new TimeSpan(now.Ticks - (new DateTime(now.Year, now.Month, now.Day).Ticks));

            // Convert to a byte array 
            // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333 
            byte[] daysArray = BitConverter.GetBytes(days.Days);
            byte[] msecsArray = BitConverter.GetBytes((long)(msecs.TotalMilliseconds / 3.333333));

            // Reverse the bytes to match SQL Servers ordering 
            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);

            // Copy the bytes into the guid 
            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

            return new System.Guid(guidArray);
        }
    }
}
