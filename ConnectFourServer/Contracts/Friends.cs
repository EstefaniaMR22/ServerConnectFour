//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Contracts
{
    using System;
    using System.Collections.ObjectModel;
    
    public partial class Friends
    {
        public int friendshipID { get; set; }
        public int player { get; set; }
        public int friend { get; set; }
    
        public virtual Players Players { get; set; }
    }
}
