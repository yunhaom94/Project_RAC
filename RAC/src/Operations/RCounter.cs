using System;
using System.Collections.Generic;
using System.Linq;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    public class RCounter : Operation<RCounterPayload>
    {

        // todo: set this to its typecode
        public override string typecode { get ; set; } = "rc";

        public RCounter(string uid, Parameters parameters, Clock clock = null) : base(uid, parameters, clock)
        {
            // todo: put any necessary data here
        }


        public override Responses GetValue()
        {
            Responses res;

            if (this.payload is null)
            {
                res = new Responses(Status.fail);
                res.AddResponse(Dest.client, "Rcounter with id {0} cannot be found");
            } 
            else
            {
                int pos = payload.PVector.Sum();
                int neg = payload.NVector.Sum();

                res = new Responses(Status.success);
                res.AddResponse(Dest.client, (pos - neg).ToString()); 
            }
            payloadNotChanged = true;
            
            return res;
        }

        public override Responses SetValue()
        {

            RCounterPayload pl = new RCounterPayload(uid, (int)Config.numReplicas, (int)Config.replicaId, this.clock);
            RCounterPayload oldstate = pl.CloneValues();

            int value = this.parameters.GetParam<int>(0);
            if (value >= 0)
                pl.PVector[pl.replicaid] = value;
            else
                pl.NVector[pl.replicaid] = -value;

            this.payload = pl;

            string opid = AddToOpHistory(oldstate, this.payload.CloneValues());

            Responses res = GenerateSyncRes();
            res.AddResponse(Dest.client, opid); 
            
            return res;
        }

        public Responses Increment()
        {
            RCounterPayload oldstate = this.payload.CloneValues();
            this.payload.PVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            string opid = AddToOpHistory(oldstate, this.payload.CloneValues());

            Responses res = GenerateSyncRes();
            res.AddResponse(Dest.client, opid);
            return res;

        }

        public Responses Decrement()
        {
            RCounterPayload oldstate = this.payload.CloneValues();
            this.payload.NVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            string opid = AddToOpHistory(oldstate, this.payload.CloneValues());

            Responses res = GenerateSyncRes();
            res.AddResponse(Dest.client, opid); 

            return res;

        }

        public override Responses Synchronization()
        {
            Responses res;

            List<int> otherP = this.parameters.GetParam<List<int>>(0);
            List<int> otherN = this.parameters.GetParam<List<int>>(1);
            
            if (this.payload is null)
            {
                RCounterPayload pl = new RCounterPayload(uid, (int)Config.numReplicas, (int)Config.replicaId, this.clock);
                this.payload = pl;
            }

            if (otherP.Count != otherN.Count || otherP.Count != payload.PVector.Count)
            {   
                res = new Responses(Status.fail);
                LOG("Sync failed for item: " + this.payload.replicaid);
                return res;
            }

            for (int i = 0; i < otherP.Count; i++)
            {
                this.payload.PVector[i] = Math.Max(this.payload.PVector[i], otherP[i]);
                this.payload.NVector[i] = Math.Max(this.payload.NVector[i], otherN[i]);
            }
            
            DEBUG("Sync successful, new value for " + this.uid + " is " +  
                    (this.payload.PVector.Sum() - this.payload.NVector.Sum()));
            
            res = new Responses(Status.success);

            return res;
        }


        public Responses Reverse()
        {
            Responses res;
            
            string opid = this.parameters.GetParam<String>(0);
            
            // perpare
            (RCounterPayload, RCounterPayload)op = this.payload.OpHistory[opid];
            RCounterPayload oldstate = op.Item1;
            RCounterPayload newstate = op.Item2;

            int diff = (newstate.PVector.Sum() - newstate.NVector.Sum()) - 
                        (oldstate.PVector.Sum() - oldstate.NVector.Sum());

            RCounterPayload pl = this.payload;

            if (diff <= 0)
                pl.PVector[pl.replicaid] += -diff;
            else
                pl.NVector[pl.replicaid] += diff;

            // effect
            res = GenerateSyncRes();
            res.AddResponse(Dest.client, opid);
            return res;
        }

        public string AddToOpHistory(RCounterPayload oldstate, RCounterPayload newstate)
        {
            this.payload.clock.Increment();
            string opid = this.payload.clock.ToString();

            this.payload.OpHistory.Add(opid, (oldstate, newstate));

            return opid;

        }

        // TODO: sync op history as well
        private Responses GenerateSyncRes()
        {
            Responses res = new Responses(Status.success);

            Parameters syncPm = new Parameters(2);
            syncPm.AddParam(0, this.payload.PVector);
            syncPm.AddParam(1, this.payload.NVector);

            string broadcast = Parser.BuildCommand(this.typecode, "y", this.uid, syncPm);
            
            res.AddResponse(Dest.broadcast, broadcast, false);
            return res;
        }

    }



}

