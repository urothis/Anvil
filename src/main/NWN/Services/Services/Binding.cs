using System;

namespace NWN.Services
{
  public class Binding
  {
    public readonly Type ImplementationType;
    public readonly ServiceBindingAttribute BindingInfo;

    public Binding(Type implementationType, ServiceBindingAttribute bindingInfo)
    {
      this.ImplementationType = implementationType;
      this.BindingInfo = bindingInfo;
    }
  }
}