using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Dunet;

namespace Envy;

[Union]
internal partial record ModelMember
{
    partial record Property( PropertyInfo Info );

    partial record Field( FieldInfo Info );

    public string Name => this switch
    {
        Property p => p.Info.Name,
        Field f    => f.Info.Name,
        _          => throw new NotImplementedException(),
    };

    public Type Type => this switch
    {
        Property p => p.Info.PropertyType,
        Field f    => f.Info.FieldType,
        _          => throw new NotImplementedException(),
    };

    public bool IsRequired => this switch
    {
        Property p => p.Info.GetCustomAttribute<RequiredAttribute>() is not null
                   || p.Info.GetCustomAttribute<RequiredMemberAttribute>() is not null,
        Field f => f.Info.GetCustomAttribute<RequiredAttribute>() is not null
                || f.Info.GetCustomAttribute<RequiredMemberAttribute>() is not null,
        _ => throw new NotImplementedException(),
    };

    public bool IsOptedOut => this switch
    {
        Property p => p.Info.GetCustomAttribute<IgnoreDataMemberAttribute>() is not null,
        Field f    => f.Info.GetCustomAttribute<IgnoreDataMemberAttribute>() is not null,
        _          => throw new NotImplementedException(),
    };

    public bool IsNullable
    {
        get
        {
            var nullableProp = this switch
            {
                Property p => p.Info.PropertyType.GetCustomAttribute<NullableAttribute>() is not null,
                Field f    => f.Info.FieldType.GetCustomAttribute<NullableAttribute>() is not null,
                _          => throw new NotImplementedException(),
            };

            return nullableProp
                || ( this.Type is { IsValueType: true, IsConstructedGenericType: true }
                  && this.Type.GetGenericTypeDefinition() == typeof( Nullable<> ) );
        }
    }

    public bool IsOptedIn => this switch
    {
        Property p => p.Info.GetCustomAttribute<DataMemberAttribute>() is not null,
        Field f    => f.Info.GetCustomAttribute<DataMemberAttribute>() is not null,
        _          => throw new NotImplementedException(),
    };

    public IEnumerable<Attribute> GetCustomAttributes( bool inherit = true ) => this switch
    {
        Property p => p.Info.GetCustomAttributes( inherit ).Cast<Attribute>(),
        Field f    => f.Info.GetCustomAttributes( inherit ).Cast<Attribute>(),
        _          => [],
    };

    public object? GetValue( object obj ) => this switch
    {
        Property p => p.Info.GetValue( obj ),
        Field f    => f.Info.GetValue( obj ),
        _          => throw new NotImplementedException(),
    };

    public void SetValue( object obj, object? value )
    {
        switch( this )
        {
            case Property p:
                p.Info.SetValue( obj, value );
                break;
            case Field f:
                f.Info.SetValue( obj, value );
                break;
            default:
                throw new NotImplementedException();
        }
    }
}
