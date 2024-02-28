using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaveGamblingContactXAM.Models
{
    public class ContactModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; } = string.Empty;
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Date { get; set; }
        public bool IsRegistered { get; set; }
        public string IdGroup { get; set; }
        public string UserLine { get; set; }
    }
}
