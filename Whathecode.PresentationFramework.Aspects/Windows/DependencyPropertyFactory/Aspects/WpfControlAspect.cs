﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using Whathecode.System.Reflection.Extensions;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;
using Visibility = PostSharp.Reflection.Visibility;


namespace Whathecode.System.Windows.DependencyPropertyFactory.Aspects
{
	/// <summary>
	///   Aspect which when applied to a DependencyObject, allows using the <see cref = "DependencyPropertyFactory" /> without having to
	///   add it to the class, or delegate calls to it in the property getters and setters.
	/// </summary>
	/// <typeparam name = "T">Enum type specifying all the dependency properties.</typeparam>
	[Serializable]
	public class WpfControlAspect<T> : IInstanceScopedAspect, IAspectProvider
	{
		class ConcreteDependencyPropertyFactory : DependencyPropertyFactory<T>
		{
			public ConcreteDependencyPropertyFactory( Type ownerType )
				: base( ownerType, false, false )
			{
			}
		}


		[NonSerialized]
		object _instance;

		/// <summary>
		///   Keep track of separate factories per type in order to support generic controls.
		/// </summary>
		[NonSerialized]
		internal static readonly Dictionary<Type, DependencyPropertyFactory<T>> PropertyFactories = new Dictionary<Type, DependencyPropertyFactory<T>>();

		[IntroduceMember( Visibility = Visibility.Private )]
		public DependencyPropertyFactory<T> PropertyFactory
		{
			get
			{
				DependencyPropertyFactory<T> factory;
				PropertyFactories.TryGetValue( _instance.GetType(), out factory );
				return factory;
			}
			private set { PropertyFactories[ _instance.GetType() ] = value; }
		}


		public object CreateInstance( AdviceArgs adviceArgs )
		{
			var newAspect = (WpfControlAspect<T>)MemberwiseClone();
			newAspect._instance = adviceArgs.Instance;

			return newAspect;
		}

		public void RuntimeInitializeInstance()
		{
			if ( PropertyFactory == null )
			{
				PropertyFactory = new ConcreteDependencyPropertyFactory( _instance.GetType() );
			}
		}

		public IEnumerable<AspectInstance> ProvideAspects( object targetElement )
		{
			var targetType = (Type)targetElement;

			Dictionary<MemberInfo, DependencyPropertyAttribute[]> attributedProperties
				= targetType.GetAttributedMembers<DependencyPropertyAttribute>( MemberTypes.Property );

			return from member in attributedProperties
				   let attribute = member.Value[ 0 ] let propertyAspect = new DependencyPropertyAspect<T>( (T)attribute.GetId() )
				   select new AspectInstance( member.Key, propertyAspect );
		}

		public static DependencyProperty GetDependencyProperty( Type type, T property )
		{
			return PropertyFactories[ type ][ property ];
		}
	}
}