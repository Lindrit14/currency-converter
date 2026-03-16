using CoreWCF;
using currency_converter.Domain;

namespace currency_converter.Hosting.Soap;

public static class SoapFaultMapper
{
    public static FaultException MapToFault(Exception ex) => ex switch
    {
        ArgumentException argEx => new FaultException(
            new FaultReason(argEx.Message),
            new FaultCode("Sender")),

        CurrencyNotFoundException cnfEx => new FaultException(
            new FaultReason(cnfEx.Message),
            new FaultCode("Sender")),

        InvalidOperationException ioEx => new FaultException(
            new FaultReason(ioEx.Message),
            new FaultCode("Receiver")),

        _ => new FaultException(
            new FaultReason("An internal error occurred."),
            new FaultCode("Receiver"))
    };
}
