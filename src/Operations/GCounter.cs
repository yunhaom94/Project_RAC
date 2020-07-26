using System;
using System.Linq;
using System.Collections;

using RAC.Payloads;
using System.Collections.Generic;
using static RAC.API;

namespace RAC.Operations
{
    public class GCounter : Operation<GCPayload>
    {

        //public GCPayload payload;

        public GCounter(string uid, Parameters parameters) : base(uid, parameters)
        {
        }


        public override Responses GetValue()
        {
            Responses res;

            if (this.payload is null)
            {
                res = new Responses(Status.fail);
                res.AddReponse(Dest.client, "Gcounter with id {0} cannot be found");
            } 
            else
            {
                res = new Responses(Status.success);
                res.AddReponse(Dest.client, payload.valueVector.Sum().ToString()); 
            }
            
            return res;
        }

        public override Responses SetValue()
        {
            Responses res = new Responses(Status.success);

            GCPayload pl = new GCPayload(uid, (int)Config.numReplicas, (int)Config.replicaId);

            

            pl.valueVector[pl.replicaid] = this.parameters.GetParam<int>(0);

            this.payload = pl;

            res.AddReponse(Dest.client); 

            Parameters syncPm = new Parameters(1);
            syncPm.AddParam(0, this.payload.valueVector);



            string broadcast = Parser.BuildCommand("gc", "y", this.uid, syncPm);
            res.AddReponse(Dest.broadcast, broadcast, false);

            
            return res;
        }

        public Responses Increment()
        {
            this.payload.valueVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            Responses res = new Responses(Status.success);

            Parameters syncPm = new Parameters(1);
            syncPm.AddParam(0, this.payload.valueVector);

            string broadcast = Parser.BuildCommand("gc", "y", this.uid, syncPm);
            
            res.AddReponse(Dest.client); 
            res.AddReponse(Dest.broadcast, broadcast, false);

            return res;

        }

        public override Responses Synchronization()
        {
            Responses res = new Responses(Status.success);

            List<int> otherState = this.parameters.GetParam<List<int>>(0);
            
            // TODO: add sanity check

            if (this.payload is null)
            {
                GCPayload pl = new GCPayload(uid, (int)Config.numReplicas, (int)Config.replicaId);
                this.payload = pl;
            }

            for (int i = 0; i < otherState.Count; i++)
            {
                this.payload.valueVector[i] = Math.Max(this.payload.valueVector[i], otherState[i]);
            }

            Console.WriteLine("sync successful");

            return res;

        }
    }


}