﻿using Protocols.Common;
using Protocols.Response;
using Serilog;
using System.Collections.Generic;

namespace ServerShared.Model
{
    public partial class Room
    {
        public int Id { get; set; }

        private int PlayerIndex { get; set; }

        private Dictionary<int, Session> Sessions { get; } = new();

        private Queue<Vector3> Positions { get; } = new();

        public Room()
        {
            Positions.Enqueue(new Vector3 { x = -3.5f, y = 0f, z = 3.5f });
            Positions.Enqueue(new Vector3 { x = 3.5f, y = 0f, z = 3.5f });
            Positions.Enqueue(new Vector3 { x = 3.5f, y = 0f, z = -3.5f });
            Positions.Enqueue(new Vector3 { x = -3.5f, y = 0f, z = -3.5f });
        }

        public bool IsEmpty()
        {
            return Sessions.Count == 0;
        }

        public bool Enter(Session session, Protocols.Request.Enter enter)
        {
            if(Sessions.ContainsValue(session))
            {
                Log.Error($"Enter() <Desc:Already Entered User> <Session:{session}>");
                return false;
            }

            session.PlayerIndex = PlayerIndex++;
            session.Room = this;

            var position = Positions.Dequeue();

            session.Position = position;

            Positions.Enqueue(position);

            foreach(var existingSession in Sessions.Values)
            {
                session.Send(new Enter
                {
                    PlayerIndex = existingSession.PlayerIndex.Value,
                    Position = existingSession.Position,
                    Name = existingSession.Name,
                });
            }

            Sessions.Add(session.PlayerIndex.Value, session);

            Broadcast(new Enter
            {
                PlayerIndex = session.PlayerIndex.Value,
                Position = session.Position,
                Name = session.Name,
            });

            return true;
        }

        public void Leave(Session session, Protocols.Request.Leave leave)
        {
            Broadcast(new Leave
            {
                PlayerIndex = session.PlayerIndex.Value,
            });

            Sessions.Remove(session.PlayerIndex.Value);
            session.PlayerIndex = null;
            session.Room = null;
        }

        public void Move(Session session, Protocols.Request.Move move)
        {
            session.Position = move.Position;

            Broadcast(new Move { Position = move.Position, PlayerIndex = session.PlayerIndex.Value });
        }

        public void Broadcast<T>(T t) where T : Header
        {
            Broadcast(Sessions.Values, t);
        }

        public void Broadcast<TList, T>(TList sessions, T t) 
            where T : Header
            where TList : IEnumerable<Session>
        {
            foreach (var session in sessions)
            {
                session.Send(t);
            }
        }
    }
}
