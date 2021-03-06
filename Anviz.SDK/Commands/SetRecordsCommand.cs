﻿using Anviz.SDK.Responses;

namespace Anviz.SDK.Commands
{
    class SetRecordsCommand : Command
    {
        private const byte SET_RECORDS = 0x41;
        public SetRecordsCommand(ulong deviceId, Record record) : base(deviceId)
        {
            BuildPayload(SET_RECORDS, record.ToArray());
        }
    }
}
