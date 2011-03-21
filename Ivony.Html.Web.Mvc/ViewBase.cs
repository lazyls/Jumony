﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web;
using System.Web.Routing;
using System.Web.Caching;
using System.IO;
using System.Web.Mvc.Html;

namespace Ivony.Html.Web.Mvc
{
  public abstract class ViewBase : IView, ICachableResult
  {

    protected ViewBase()
    {
      RenderAdapter = new PartialRenderAdapter( this );
    }


    protected string VirtualPath
    {
      get;
      set;
    }


    protected ViewContext ViewContext
    {
      get;
      private set;
    }

    protected object ViewModel
    {
      get { return ViewContext.ViewData.Model; }
    }

    protected ViewDataDictionary ViewData
    {
      get { return ViewContext.ViewData; }
    }

    protected HttpContextBase HttpContext
    {
      get { return ViewContext.HttpContext; }
    }

    protected RequestContext RequestContext
    {
      get { return ViewContext.RequestContext; }
    }

    protected RouteData RouteData
    {
      get { return ViewContext.RouteData; }
    }

    protected TempDataDictionary TempData
    {
      get { return ViewContext.TempData; }
    }

    protected Cache Cache
    {
      get { return HttpContext.Cache; }
    }


    protected UrlHelper Url
    {
      get;
      private set;
    }




    public virtual void Render( ViewContext viewContext, TextWriter writer )
    {
      ViewContext = viewContext;

      Url = new UrlHelper( viewContext.RequestContext );

      ProcessMain();

      var content = RenderContent();

      CachedResult = new ContentResult()
      {
        Content = content,
        ContentType = "text/html"
      };

      writer.Write( content );
    }


    protected abstract void ProcessMain();

    protected abstract string RenderContent();


    protected IHtmlAdapter RenderAdapter
    {
      get;
      private set;
    }


    public ActionResult CachedResult
    {
      get;
      protected set;
    }




    protected void ProcessPartials( IHtmlContainer container )
    {

    }


    protected void ProcessLinks( IHtmlContainer container )
    {

      {
        var links = container.Find( "a[action]" );

        foreach ( var actionLink in links )
        {
          var action = actionLink.Attribute( "action" ).Value();
          var controller = actionLink.Attribute( "controller" ).Value() ?? RouteData.Values["controller"];

          var routeValues = new RouteValueDictionary();

          routeValues["action"] = action;
          routeValues["controller"] = controller;


          foreach ( var attribute in actionLink.Attributes().Where( a => a.Name.StartsWith( "_" ) ).ToArray() )
          {
            routeValues.Add( attribute.Name.Substring( 1 ), attribute.Value() );
            attribute.Remove();
          }


          actionLink.Attribute( "action" ).Remove();

          var controllerAttribute = actionLink.Attribute( "controller" );
          if ( controllerAttribute != null )
            controllerAttribute.Remove();




          var href = Url.RouteUrl( routeValues );

          if ( href == null )
            actionLink.Attribute( "href" ).Remove();

          else
            actionLink.SetAttribute( "href", href );
        }


      }


      container.Find( "a[href^=~]" ).ToArray().SetAttribute( "href", url => VirtualPathUtility.ToAbsolute( url ) );


    }


    protected virtual void RenderPartial( IHtmlElement partialElement, TextWriter writer )
    {

      var action = partialElement.Attribute( "action" ).Value();
      var view = partialElement.Attribute( "view" ).Value();

      if ( action != null && view != null )
        throw new NotSupportedException( "无法处理的partial标签：" + ContentExtensions.GenerateTagHtml( partialElement, false ) );


      if ( action != null )
      {

        var controller = partialElement.Attribute( "controller" ).Value();

        var helper = MakeHelper();

        writer.Write( helper.Action( actionName: action, controllerName: controller ) );

        return;
      }


      if ( view != null )
      {

        var helper = MakeHelper();

        writer.Write( helper.Partial( partialViewName: view ) );

        return;

      }




    }

    protected HtmlHelper MakeHelper()
    {
      var helper = new HtmlHelper( ViewContext, new ViewDataContainer( this ) );
      return helper;
    }




    public virtual void Dispose()
    {
    }


    protected class ViewDataContainer : IViewDataContainer
    {

      private ViewBase _view;

      public ViewDataContainer( ViewBase view )
      {
        _view = view;
      }


      public ViewDataDictionary ViewData
      {
        get
        {
          return _view.ViewData;
        }
        set
        {
          throw new NotSupportedException();
        }
      }
    }


    public class PartialRenderAdapter : IHtmlAdapter
    {

      private ViewBase _view;

      public PartialRenderAdapter( ViewBase view )
      {
        _view = view;
      }

      public bool Render( IHtmlNode node, TextWriter writer )
      {
        var element = node as IHtmlElement;

        if ( element == null )
          return false;

        if ( element.Name != "partial" )
          return false;


        _view.RenderPartial( element, writer );
        return true;

      }
    }



  }
}
