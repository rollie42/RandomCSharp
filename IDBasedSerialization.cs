var team = new Team() { Name = "BlueTeam", Members = new List<Member>() { new Member() { Name = "Tim" }, new Member() { Name = "Bob" } } };
            var str1 = JsonConvert.SerializeObject(team);
            var str2 = JsonConvert.SerializeObject(team, new RJsonConverter());
 
            var t1 = JsonConvert.DeserializeObject<Team>(str1);
            var t2 = JsonConvert.DeserializeObject<Team>(str2, new RJsonConverter());
 
…
 
using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Reflection;
 
namespace Sandbox
{
    public class RJsonConverter : JsonConverter
    {
        public List<Base> SeenDependencies { get; set; } = new List<Base>();
        public Base OriginalObject { get; set; }
 
        public override bool CanConvert(Type objectType)
        {
            return (typeof(Base).IsAssignableFrom(objectType));
        }
 
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return new Member();
            }
 
            var b = Activator.CreateInstance(objectType);
            serializer.Populate(reader, b);
            return b;
        }
 
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Converters.Remove(this);
            serializer.Converters.Add(new RRJsonConverter(this));
 
            OriginalObject = (Base)value;
 
            var jo = new JObject();
            foreach (PropertyInfo prop in value.GetType().GetProperties())
            {
                if (prop.CanRead)
                {
                    object propVal = prop.GetValue(value, null);
                    if (propVal != null)
                    {
                        jo.Add(prop.Name, JToken.FromObject(propVal, serializer));
                    }
                }
            }
            jo.WriteTo(writer);
        }
    }
 
    public class RRJsonConverter : JsonConverter
    {
        public RJsonConverter ParentConverter { get; set; }
 
        public RRJsonConverter(RJsonConverter parentConverter)
        {
            ParentConverter = parentConverter;
        }
 
        public override bool CanRead
        {
            get { return true; }
        }
 
        public override bool CanConvert(Type objectType)
        {
            return (typeof(Base).IsAssignableFrom(objectType));
        }
 
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var b = (Base)Activator.CreateInstance(objectType);
            var id = serializer.Deserialize<string>(reader);
            b.Id = id;
            return b;
        }
 
        public override void WriteJson(JsonWriter writer, object objValue, JsonSerializer serializer)
        {
            var value = (Base)objValue;
            if (value != ParentConverter.OriginalObject && !ParentConverter.SeenDependencies.Any(d => d.Id == value.Id))
            {
                ParentConverter.SeenDependencies.Add(value);
            }
 
            var t = JToken.FromObject(value.Id);
            t.WriteTo(writer);
        }
    }
 
    public class Base
    {
        public string Id {
            get
            {
                return "1234";
            }
 
            set
            {
 
            }
        }
    }
 
    public class Team : Base
    {
        public Team()
        {
            Members = new List<Member>();
            Owner = new Member() { Name = "Ralph" };
        }
 
        public string Name { get; set; }
        public Member Owner { get; set; }
        public List<Member> Members { get; set; }
    }
 
    public class Member : Base
    {
        public string Name { get; set; }
    }
}