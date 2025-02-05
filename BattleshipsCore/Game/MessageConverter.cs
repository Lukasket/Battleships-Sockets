﻿using BattleshipsCore.Data;
using BattleshipsCore.Game.GameGrid;
using BattleshipsCore.Game.PlaceableObjects;
using BattleshipsCore.Interfaces;
using BattleshipsCore.Requests;
using BattleshipsCore.Responses;
using BattleshipsCore.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

#nullable disable

namespace BattleshipsCore.Game
{
    public class ConcreteMessageConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(Message).IsAssignableFrom(objectType) && !objectType.IsAbstract) return null;

            return base.ResolveContractConverter(objectType);
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);

            contract.ItemRequired = Required.AllowNull;

            return contract;
        }
    }

    public class ConcreteCommandConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(Request).IsAssignableFrom(objectType) && !objectType.IsAbstract) return null;

            return base.ResolveContractConverter(objectType);
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);

            contract.ItemRequired = Required.AllowNull;

            return contract;
        }
    }

    public class MessageConverter : JsonConverter
    {
        static JsonSerializerSettings ResponseConverterSettings = new() { ContractResolver = new ConcreteMessageConverter() };
        static JsonSerializerSettings RequestConverterSettings = new() { ContractResolver = new ConcreteCommandConverter() };

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Message) || objectType == typeof(Request);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            var messageCode = jo[Message.MessageTypeProperty]!.Value<int>();
            var enumType = (MessageType)messageCode;

            return enumType switch
            {
                MessageType.Ok => ToMessage<OkResponse>(jo),
                MessageType.Fail => ToMessage<FailResponse>(jo),

                MessageType.SendPlayerList => ToMessage<SendPlayerListResponse>(jo),
                MessageType.SendSessionKey => ToMessage<SendSessionKeyResponse>(jo),
                MessageType.SendSessionList => ToMessage<SendSessionListResponse>(jo),
                MessageType.SendSessionData => ToMessage<SendSessionDataResponse>(jo),
                MessageType.SendMapData => ToMessage<SendMapDataResponse>(jo),
                MessageType.SendTileUpdate => ToMessage<SendTileUpdateResponse>(jo),
                MessageType.SendText => ToMessage<SendTextResponse>(jo),
                MessageType.NewSessionsAdded => ToMessage<NewSessionsAddedResponse>(jo),

                MessageType.GetPlayerList => ToCommand<GetPlayerListRequest>(jo),
                MessageType.GetSessionList => ToCommand<GetSessionListRequest>(jo),
                MessageType.GetSessionData => ToCommand<GetSessionDataRequest>(jo),
                MessageType.GetMapData => ToCommand<GetMapDataRequest>(jo),
                MessageType.GetOpponentMap => ToCommand<GetOpponentMapRequest>(jo),
                MessageType.GetMyTurn => ToCommand<GetMyTurnRequest>(jo),

                MessageType.JoinServer => ToCommand<JoinServerRequest>(jo),
                MessageType.JoinedServer => ToMessage<JoinedServerResponse>(jo),

                MessageType.Disconnect => ToCommand<DisconnectRequest>(jo),
                MessageType.Disconnected => ToMessage<DisconnectResponse>(jo),

                MessageType.JoinSession => ToCommand<JoinSessionRequest>(jo),
                MessageType.JoinedSession => ToMessage<JoinedSessionResponse>(jo),

                MessageType.LeaveSession => ToCommand<LeaveSessionRequest>(jo),
                MessageType.LeftSession => ToMessage<LeftSessionResponse>(jo),

                MessageType.CreateSession => ToCommand<CreateSessionRequest>(jo),

                MessageType.StartGame => ToCommand<StartGameRequest>(jo),
                MessageType.StartedGame => ToMessage<StartedGameResponse>(jo),

                MessageType.StartBattle => ToCommand<StartBattleRequest>(jo),
                MessageType.StartedBattle => ToMessage<StartedBattleResponse>(jo),

                MessageType.SetTiles => HandleSetTiles(jo),
                MessageType.UnsetTiles => ToCommand<UnsetTilesRequest>(jo),
                MessageType.Shoot => ToCommand<ShootRequest>(jo),

                MessageType.ActiveTurn => ToMessage<ActiveTurnResponse>(jo),
                MessageType.InactiveTurn => ToMessage<InactiveTurnResponse>(jo),
                MessageType.WonGame => ToMessage<WonGameResponse>(jo),
                MessageType.LostGame => ToMessage<LostGameResponse>(jo),

                _ => throw new UnknownMessageException($"Unknown message with code: {messageCode};")
            };
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException(); // won't be called because CanWrite returns false
        }

        private TMessage ToMessage<TMessage>(JObject val) where TMessage : Message
        {
            return JsonConvert.DeserializeObject<TMessage>(val.ToString(), ResponseConverterSettings);
        }

        private TCommand ToCommand<TCommand>(JObject val) where TCommand : Request
        {
            return JsonConvert.DeserializeObject<TCommand>(val.ToString(), RequestConverterSettings);
        }

        // Should look for a better solution, this is an exception
        private SetTilesRequest HandleSetTiles(JObject val)
        {
            var playerName = val.Value<string>("PlayerName");
            var placeableObjects = new List<PlacedObject>();

            foreach (var item in val["PlacedObjects"])
            {
                var tiles = JsonConvert.DeserializeObject<List<Vec2>>(item["Tiles"].ToString());

                var objVal = item["Obj"];               
                if(objVal.Value<int>("ShipType") > 3)
                {
                    Level2Factory lv = new Level2Factory();
                    var ships = lv.CreateLevel2(objVal.Value<int>("ShipType"), objVal.Value<string>("Name"), objVal.Value<int>("Length"), objVal.Value<int>("MaximumCount"));
                    ships.GenerateShip();
                    placeableObjects.Add(new PlacedObject(ships, tiles));
                }
                else
                {
                    Level1Factory lv = new Level1Factory();
                    var ships = lv.CreateLevel1(objVal.Value<int>("ShipType"), objVal.Value<string>("Name"), objVal.Value<int>("Length"), objVal.Value<int>("MaximumCount"));
                    ships.GenerateShip();
                    placeableObjects.Add(new PlacedObject(ships, tiles));
                }  
            }

            var request = new SetTilesRequest(playerName, placeableObjects);

            return request;
        }
    }
}
