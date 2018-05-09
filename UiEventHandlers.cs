// ***************************************************************************************************************
// UiEventHandlers.cs
// 
// 03/17/2014   8:39 AM
// 
// 
// Copyright (C) 2009-2014 Oceanside Software Corporation (R)  Dallas, Texas
// All Rights Reserved, Contact 214-484-9559 for Engineering Support or
// licensing questions.  www.oneposretail.com
// 
// THIS HEADER MUST REMAIN IN ALL SOURCE FILES, DO NOT REMOVE IT.
// 
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT
// WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING 
// BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND/OR FITNESS FOR A PARTICULAR PURPOSE.  YOU MUST HAVE A 
// COMMERCIAL LICENSE TO MODIFY THIS CODE OR REDISTRIBUTE 
// BINARY FILES DERIVED FROM THIS CODE.  SOURCE CODE MAY NOT
// BE REDISTRIBUTED.
// 
// Oceanside POS and Oceanside Software Corporation are Registered 
// Trademarks of Oceanside Software Corporation.  No right is given to 
// use any trademarks owned by Oceanside Software.
// 
// Author  : Jason T. Brower                                    
// ***************************************************************************************************************

#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using Onepos.Portable.Pos.Core;
using Onepos.Portable.Pos.Core.Types;
using Onepos.Portable.Pos.Model;
using Onepos.Portable.Pos.Settings;
using Onepos.Pos.App.Desktop.DepInjection;
using Onepos.Pos.App.Desktop.Helpers;
using Onepos.Pos.BLL;
using Onepos.Pos.Core;
using Onepos.Pos.Core.Interfaces;
using Onepos.Pos.Display;
using Onepos.Pos.Dto;
using Onepos.Pos.Persistence.EntityFramework;
using Onepos.Pos.Persistence.Static;
using Onepos.Pos.Wpf.Controls;
using Onepos.Pos.Wpf.Controls.Buttons;
using Onepos.Pos.Wpf.Controls.ListBox.Discount;
using Onepos.Pos.Wpf.Controls.ListBox.Payment;
using Onepos.Pos.Wpf.Controls.MessageBox;
using Bandaid = Onepos.Pos.Core.Bandaid;
using Onepos.Pos.Module.Backoffice.ViewModels;
using Onepos.Pos.Module.Backoffice.InnverViews;
using Onepos.Pos.Module.MenuBoard;

#endregion

namespace Onepos.Pos.App.Desktop.Events
{
    /// <summary>
    ///     This class will handle many of the UI events that are called when a product is pressed, product group pressed, a
    ///     sale is
    ///     added, a check is updated and ect.  Essentailly it will handle most of the events for the vanilla buttons that are
    ///     not
    ///     directly handled in a depedency injection via the Bindings Factory.
    /// </summary>
    internal class UiEventHandlers
    {
        /// <summary>
        ///     A user has touch a vanilla button in a list that has a datacontext
        ///     of a SaleDto.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        public static void SaleDtoTouched(object sender, PosEventArgs a)
        {
            //If the user is in the middle of a pending action such as adding forced modifiers, don't allow 
            //the user to escape until that operation is complete.
            if (DisplayController.IsPendingForcedModifierSelection) return;
            var salesViewContainer = a.DataSource as OrderedSalesViewContainer;
            var saleDto = salesViewContainer != null ? salesViewContainer.SaleDto : a.DataSource as ISaleDto;
            if (saleDto != null)
            {
                //If this is a modifier, then select the parent sale instead of the
                //modifier sale.
                if (saleDto.IsModifier)
                {
                    saleDto = saleDto.ParentSaleWhenModSaleDto ?? saleDto;
                }
                saleDto.IsSelected = !saleDto.IsSelected;
                if (saleDto.IsSelected)
                {

                    // DisplayController.MenuBoardView.SelectedSalesContainer.Sales.Where(p => p.IsSelected = false);
                    DisplayController.MenuBoardView.AddSaleToSellectedList(saleDto);


                    DisplayController.MenuBoardView.SelectSale(saleDto);
                }
                else
                {
                    DisplayController.MenuBoardView.SelectedSalesList().Remove(saleDto);
                }
            }
        }

        /// <summary>
        ///     A user has touch a vanilla button in a list that has a datacontext
        ///     of a ProductDto.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        public static void ProductDtoTouched(object sender, PosEventArgs a)
        {

            SelectProduct(a.DataSource);
           

        }

        /// <summary>
        ///     A user has touch a vanilla button in a list that has a datacontext
        ///     of a ProductGroupDto.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        public static void ProductGroupDtoTouched(object sender, PosEventArgs a)
        {
            SelectGroup(a.DataSource);
        }

        /// <summary>
        ///     A user has touch a vanilla button in a list that has a datacontext
        ///     of a ForceModifierDto.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        public static void ForcedModifierDtoTouched(object sender, PosEventArgs a)
        {
            SelectProduct(a.DataSource);
        }

        /// <summary>
        ///     A user has touch a vanilla button in a list that has a datacontext
        ///     of a ForceModifierGroupDto.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        public static void ForcedModifierGroupDtoTouched(object sender, PosEventArgs a)
        {
            SelectGroup(a.DataSource);
        }

        /// <summary>
        ///     A user has touch a vanilla button in a list that has a datacontext
        ///     of a ExceptionModifierDto.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        public static void ExceptionModifierDtoTouched(object sender, PosEventArgs a)
        {
            SelectProduct(a.DataSource);
        }

        /// <summary>
        ///     A user has touch a vanilla button in a list that has a datacontext
        ///     of a ExceptionModifierGroupDto.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        public static void ExceptionModifierGroupDtoTouched(object sender, PosEventArgs a)
        {
            SelectGroup(a.DataSource);
        }


        /// <summary>
        ///     This event hanlder is tied to the menu screen.  When the user is looking at the
        ///     screen in check mode and they expand the list of payments, then touch a payment,
        ///     this event will fire popping open a payments control where they can perform actions
        ///     on the payments.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        public static void PaymentsViewContainerTouched(object sender, PosEventArgs a)
        {
            var session = Bandaid.SessionService.ActiveSession;
            if (session == null) return;

            var order = session.SelectedOrder;
            if (order == null) return;

            var container = DisplayController.MenuBoardView.SelectedSalesContainer as CheckContainerDto;
            if (container == null) return;

            var pb = new PaymentsBox(order, container.SeatNo, session.CanAcceptTips);
            Bandaid.DialogService.DisplayControl(pb);
        }

        /// <summary>
        ///     This event hanlder is tied to the menu screen.  When the user is looking at the
        ///     screen in check mode and they expand the list of discounts, then touch a discount,
        ///     this event will fire popping open a discounts control where they can perform actions
        ///     on the discounts.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        public static void DiscountsViewContainerTouched(object sender, PosEventArgs a)
        {
            var session = Bandaid.SessionService.ActiveSession;
            if (session == null) return;

            var order = session.SelectedOrder;
            if (order == null) return;

            var container = DisplayController.MenuBoardView.SelectedSalesContainer as CheckContainerDto;
            if (container == null) return;

            var db = new DiscountsBox(order, container.SeatNo);
            Bandaid.DialogService.DisplayControl(db);
        }


        /// <summary>
        ///     Occurs when the user selects an open order from the desktop page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        public static void OrderDtoTouched(object sender, PosEventArgs a)
        {
            var orderDetails = a.DataSource as OrderDetails;
            if (orderDetails != null)
            {
                DisplayController.MenuBoardView.AddOrder(orderDetails.Id);
            }
            DisplayController.UpdateState(ApplicationCommand.CmdSelectMenuBoardScreen);
        }

        public static void SalesSelectionCleared(object sender, PosEventArgs a)
        {
            DisplayController.UpdateState(ApplicationCommand.CmdUpdateMenuBoardDependencyInjections);
        }

        public static void ContainerSelectionChanged(object sender, PosEventArgs a)
        {
            foreach (var sale in DisplayController.MenuBoardView.SelectedSalesContainer.Sales)
            {
                sale.IsSelected = false;
            }

            DisplayController.MenuBoardView.SelectedSalesContainer.ReconstructSalesView();
            DisplayController.UpdateState(ApplicationCommand.CmdUpdateMenuBoardDependencyInjections);

            if (!Portable.Pos.Core.Bandaid.ConfigurationService.HasCustomerFacingDisplay) return;
            var check = DisplayController.MenuBoardView.SelectedSalesContainer as CheckContainerDto;
            if (check != null)
                PoleDisplayLogic.UpdatePoleDisplayTotals(check);
        }

        /// <summary>
        /// This function will decrement the product count for any forced modifier, exception modifier or
        /// product that is in the database and has the bit set for RemoveAtZeroCount.  This function 
        /// properly handles concurrency exceptions.
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        private static bool DecrementProductCount(IProductDto product)
        {
            if (!product.RemoveAtZeroCount) return true;

            using (var ctx = new DominicaCtx())
            {
                bool saveFailed;
                //Run in a loop to handle concurrency exceptions         
                do
                {
                    try
                    {
                        saveFailed = false;

                        if (product.IsForcedModifier)
                        {
                            var fm = ctx.ForcedModifiers.Find(product.Id);
                            if (fm == null) return false;
                            ctx.Refresh(fm);
                            if (fm.Downtick < 1) return false;
                            fm.Downtick -= 1;
                            ctx.SaveChanges();
                        }
                        else if (product.IsExceptionModifier)
                        {
                            var em = ctx.ExceptionModifiers.Find(product.Id);
                            if (em == null) return false;

                            ctx.Refresh(em);
                            if (em.Downtick < 1) return false;
                            em.Downtick -= 1;
                            ctx.SaveChanges();
                        }
                        else
                        {
                            var prod = ctx.Products.Find(product.Id);
                            if (prod == null) return false;
                            ctx.Refresh(prod);
                            if (prod.Downtick < 1) return false;
                            prod.Downtick -= 1;
                            ctx.SaveChanges();
                        }
                    }
                    catch (DbUpdateConcurrencyException e)
                    {
                        saveFailed = true;
                        Portable.Pos.Core.Bandaid.ExceptionService.Log(e);
                        Company.Portable.Core.Static.FileLogger.QueueBootMessage(e.ToString(), HardCoded.BootupLogFileName);
                    }
                    //Here we sleep for a random time before we either exit the thread or try 
                    //again.  This helps asure that all threads don't get on the same attempt in time 
                    //to lock and unlock.
                    var sleepy = new Random().Next(0, 200);
                    Thread.Sleep(sleepy);
                } while (saveFailed);
            }
            return true;
        }


        public static void ManageSales(List<SaleDto> sales)
        {
            var seat = DisplayController.MenuBoardView.SelectedSalesContainer;
            foreach (var sale in sales)
            {
                seat.Sales.Add(sale);


                Bandaid.SessionService.ActiveSession.PosModel.Sales.Add(sale.Model);
                /// DisplayController.MenuBoardView.SelectSale(sale);
                /// DisplayController.UpdateState(ApplicationCommand.CmdUpdateMenuBoardDependencyInjections);

                if (Bandaid.SessionService.ActiveSession.SelectedOrder.ChecksPresentationContainer.Count == 0)
                {
                    var container = new CheckContainerDto(Bandaid.SessionService.ActiveSession.SelectedOrder) { SeatNo = 1 };
                    container.Sales.Add(sale);
                    Bandaid.SessionService.ActiveSession.SelectedOrder.ChecksPresentationContainer.Add(container);
                }
                else
                {
                    Bandaid.SessionService.ActiveSession.SelectedOrder.ChecksPresentationContainer.First()
                        .Sales.Add(sale);
                    Bandaid.EventAggregator.GetEvent<OnSalesAddedToOrder>().Publish(seat);
                }
                //DisplayController.MenuBoardView.ScrollToLastSoldSale();
                //DisplayController.MenuBoardView.TicketControlPanelSelectSalesContainer(Bandaid.SessionService.ActiveSession.SelectedOrder.SalesContainerCollection.First());
            }
            //Publish the event that a sale was added.

            var ctx = Onepos.Pos.Core.Bandaid.SessionService.ActiveSession.PosModel;
            ctx.SaveChanges();
            DisplayController.MenuBoardView.EnterCheckMode(1);
          
            
        }

        /// <summary>
        ///     This function will add the sale of a product to the current seat/order.
        /// </summary>
        /// <param name="product"></param>
        /// 
        private int countref = 0;

        public static bool PromptForPriceOnWeight(SaleDto sale = null, IProductDto product = null)
        {
            bool validprod = true;
            var _price = 0.0M;
            var _actualprice = 0.0M;
            var order = Bandaid.SessionService.ActiveSession.SelectedOrder;
            var selectedsaleid = DisplayController.MenuBoardView.SelectedSalesContainer.Sales.FirstOrDefault(x => x.IsSelected == true);

            if (selectedsaleid != null)
            {
                var ChkSaleProduct = order.Model.Sales.FirstOrDefault(x => x.ProductId == product.Id && x.Id == selectedsaleid.Id && x.OrderId == order.Id && x.IsSelected == true);
                if (ChkSaleProduct != null)
                {
                    if (product.UseWeightPricing)
                    {
                        sale.MenuPrice = ChkSaleProduct.MenuPrice;
                        sale.DiscountPrice = ChkSaleProduct.DiscountPrice;
                        sale.AdjustPrice = ChkSaleProduct.AdjustPrice;
                        sale.salemetadata = ChkSaleProduct.salemetadata;
                        sale.ProductName = ChkSaleProduct.ProductName;
                        sale.ProductId = ChkSaleProduct.ProductId;

                        sale.Model.ProductId = ChkSaleProduct.ProductId;
                        sale.Model.MenuPrice = ChkSaleProduct.DiscountPrice;
                        sale.Model.Name = ChkSaleProduct.ProductName;
                        sale.Model.OrderId = sale.OrderId;

                        var session = Core.Bandaid.SessionService.ActiveSession;

                        if (sale.TaxConfigurationClones != null)
                        {
                            var taxGroup = session.PosModel.TaxGroups.Find(product.TaxGroupId);
                            if (taxGroup != null)
                            {
                                foreach (var taxConfiguration in taxGroup.TaxConfigurations)
                                {

                                    if (SaleDto.TaxIsActive(taxConfiguration))
                                    {
                                        var clone = new TaxConfigurationClone
                                        {
                                            IsInclusiveTax = taxConfiguration.IsInclusiveTax,
                                            Name = taxConfiguration.Name,
                                            Rate = (taxConfiguration.Rate / 100M),
                                            IsFlatFee = taxConfiguration.IsFlatFee,
                                            FlatFee = taxConfiguration.FlatFee,
                                            Store_Id = Core.Bandaid.SessionService.ActiveSession.Store.Id,
                                            ModifiedBy = Core.Bandaid.SessionService.ActiveSession.UserId.ToString(),
                                            ModifiedDate = DateTime.Now,
                                            CreatedBy = Core.Bandaid.SessionService.ActiveSession.UserId.ToString(),
                                            CreatedDate = DateTime.Now,

                                        };
                                        session.PosModel.TaxConfigurationClones.Add(clone);
                                        sale.Model.TaxConfigurationClones.Add(clone);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                _price = product.Price;
                _actualprice = product.ActualPrice;
                var mb1 = new CustomProductPriceList(product, _price, sale.Id.ToString(), order.Id.ToString());
                Bandaid.DialogService.DisplayControl(mb1);
                if (mb1.Response != QuestionResponse.Yes || !(mb1.TotalCost > 0)) return false;
                // Bypass sale id from Metadata Directly
                // saleid is foriegn key in Metadata table
                sale.MenuPrice = mb1.TotalCost;
                sale.DiscountPrice = mb1.TotalCost;
                sale.salemetadata = mb1._salemetadata;
                sale.ProductName = mb1._ProductName;
                sale.ProductId = mb1._ProductId;

                sale.Model.ProductId = mb1._ProductId;
                sale.Model.MenuPrice = mb1.TotalCost;
                sale.Model.AdjustPrice = mb1.TotalCost;
                sale.Model.DiscountPrice = mb1.TotalCost;
                sale.Model.Name = mb1._ProductName;
                sale.Model.OrderId = sale.OrderId;

                var session = Core.Bandaid.SessionService.ActiveSession;

                if (mb1._TaxGroupID != null)
                {
                    var taxGroup = session.PosModel.TaxGroups.Find(mb1._TaxGroupID);
                    if (taxGroup != null)
                    {
                        foreach (var taxConfiguration in taxGroup.TaxConfigurations)
                        {

                            if (SaleDto.TaxIsActive(taxConfiguration))
                            {
                                var clone = new TaxConfigurationClone
                                {
                                    IsInclusiveTax = taxConfiguration.IsInclusiveTax,
                                    Name = taxConfiguration.Name,
                                    Rate = (taxConfiguration.Rate / 100M),
                                    IsFlatFee = taxConfiguration.IsFlatFee,
                                    FlatFee = taxConfiguration.FlatFee,
                                    Store_Id = Core.Bandaid.SessionService.ActiveSession.Store.Id,
                                    ModifiedBy = Core.Bandaid.SessionService.ActiveSession.UserId.ToString(),
                                    ModifiedDate = DateTime.Now,
                                    CreatedBy = Core.Bandaid.SessionService.ActiveSession.UserId.ToString(),
                                    CreatedDate = DateTime.Now,

                                };
                                session.PosModel.TaxConfigurationClones.Add(clone);
                                sale.Model.TaxConfigurationClones.Add(clone);
                            }
                        }
                    }
                }

            }
            product.Price = _price;
            return validprod;
        }

        public static bool PromptForMetadataDetails(SaleDto sale = null, IProductDto product = null)
        {
            bool validprod = true;
            var _price = 0.0M;
            var _actualprice = 0.0M;
            var order = Bandaid.SessionService.ActiveSession.SelectedOrder;
            var selectedsaleid = DisplayController.MenuBoardView.SelectedSalesContainer.Sales.FirstOrDefault(x => x.IsSelected == true);

            if (selectedsaleid != null)
            {
                var ChkSaleProduct = order.Model.Sales.FirstOrDefault(x => x.ProductId == product.Id && x.Id == selectedsaleid.Id && x.OrderId == order.Id && x.IsSelected == true);
                if (ChkSaleProduct != null)
                {
                    if (product.PromptForMetaData)
                    {
                        sale.MenuPrice = ChkSaleProduct.MenuPrice;
                        sale.DiscountPrice = ChkSaleProduct.DiscountPrice;
                        sale.salemetadata = ChkSaleProduct.salemetadata;
                        sale.ProductName = ChkSaleProduct.ProductName;
                    }
                }
            }

            else
            {
                _price = product.Price;
                _actualprice = product.ActualPrice;
                var mb1 = new CustomMetaDataProducts(product, _price, sale.Id.ToString(), order.Id.ToString());
                Bandaid.DialogService.DisplayControl(mb1);
                if (mb1.Response != QuestionResponse.Yes || !(mb1.TotalCost > 0)) return false;
                // Bypass sale id from Metadata Directly
                // saleid is foriegn key in Metadata table
                sale.MenuPrice = mb1.TotalCost;
                sale.DiscountPrice = mb1.TotalCost;
                sale.AdjustPrice = mb1.TotalCost;
                sale.salemetadata = mb1._salemetadata;
                sale.ProductName = mb1._ProductName;
            }
            product.Price = _price;
            return validprod;
        }




        public static bool PromptForPriceDetails(SaleDto sale = null, IProductDto product = null, bool isdynamic = false)
        {
            //
            SaleDto saleInfo = DisplayController.MenuBoardView.Search().Tag as SaleDto;
            bool validprod = true;
            var _price = 0.0M;
            var _actualprice = 0.0M;
            var order = Bandaid.SessionService.ActiveSession.SelectedOrder;
            //var selectedsaleid = DisplayController.MenuBoardView.SelectedSalesContainer.Sales.FirstOrDefault(x => x.IsSelected == true);
            if (saleInfo != null && isdynamic)
            {
                // var ChkSaleProduct = order.Model.Sales.FirstOrDefault(x => x.ProductId == product.Id && x.Id == selectedsaleid.Id && x.OrderId == order.Id && x.IsSelected == true);
                if (product.PromptForPrice && saleInfo != null)
                {
                    sale.MenuPrice = saleInfo.MenuPrice;
                    sale.DiscountPrice = saleInfo.DiscountPrice;
                    sale.AdjustPrice = saleInfo.AdjustPrice;
                }
            }
            else
            {
                _price = product.Price;
                _actualprice = product.ActualPrice;
                var mb = new MessageBoxCashMoney(0M, CashBoxType.PayIn);
                // Bandaid.ExceptionService.Log(0);
                Bandaid.DialogService.DisplayControl(mb);
                if (mb.Response != QuestionResponse.Yes || (mb.TotalAmount == 0.0M)) return false;
                sale.MenuPrice = mb.TotalAmount;
                sale.DiscountPrice = mb.TotalAmount;
                sale.AdjustPrice = mb.TotalAmount;
            }
            product.Price = _price;
            return validprod;
        }

        public static ISaleDto AddSale(IProductDto product, DayPart daypart = null, bool isdynamic = false)
        {
            try
            {
                long count = 1;
                if (DisplayController.MenuBoardView.SelectedSalesContainer == null) return null;

                /*
                 if (product.RemoveAtZeroCount && !DecrementProductCount(product))
                 {
                     Bandaid.DialogService.ShowMessage("This product has been sold out.");
                     return null;
                 }*/

                // get onhand quantity of the product in inventory model 


                var selectedsales = DisplayController.MenuBoardView.SelectedSalesContainer;
                var order = Bandaid.SessionService.ActiveSession.SelectedOrder;
                // create temp variables to store product original price and amount 
                Sale _sale = new Sale();
                //_sale.Id = Guid.NewGuid();
                var _price = 0.0M;
                var _actualprice = 0.0M;


                //if (product.PriceByWeight)
                //{
                //    bool didRead;
                //    var totalPrice = Scale.GetScalePrice(product, out didRead);
                //    if (!didRead) return null;
                //    product.ActualPrice = totalPrice;
                //    product.Price = totalPrice;
                //}

                var seat = DisplayController.MenuBoardView.SelectedSalesContainer;

                var dayPart = new DayPart();
                if (daypart == null)
                    dayPart = Data.GetNowDaypart(Bandaid.SessionService.ActiveSession.PosModel);
                else
                    dayPart = daypart;

                // var test = new SaleDto(product, dayPart.Id);
                if (product.GroupId == Guid.Empty)
                {
                    Bandaid.DialogService.ShowMessage("Please search by barcode/click on filtered product");
                    return null;
                }

                var sale = new SaleDto(product, dayPart.Id)
                {

                    ProductIsDiscountable = !product.IsGiftCardSale
                                            && Bandaid.SessionService.ActiveSession.PosModel.ProductGroups.Find(
                                                product.GroupId).CanDiscountGroup,
                    OwningSeatNumber = seat.SeatNo,
                    SeatSequenceNumber = seat.Sales.Count + 1,
                    OwningCheckNumber = seat.Sales.Count == 0 ? 1 : seat.Sales.First().OwningCheckNumber,
                    IsSelected = true
                };   //akshay design change

                List<InventoryCountLog> inv = new List<InventoryCountLog>();
                if (product.QsrName == "Edit" && product.Price > 0 && !product.PromptForMetaData && !product.PromptForPrice)
                {
                    inv = Bandaid.SessionService.ActiveSession.PosModel.InventoryCountLogs.Where(p => p.ProductId == product.Id && p.MRPPerUnit == product.Price).ToList();
                    // inv = inv.Where(P => P.MRPPerUnit == product.Price).ToList();
                }
                else
                    inv = Bandaid.SessionService.ActiveSession.PosModel.InventoryCountLogs.Where(p => p.ProductId == product.Id).ToList();

                long ProductsCount = 0;
                if (inv != null)
                {
                    if (inv.Count > 0)
                    {
                        var selectedsaleid = DisplayController.MenuBoardView.SelectedSalesContainer.Sales.FirstOrDefault(x => x.IsSelected == true);
                        ProductsCount = inv.Sum(p => (int)p.OnHandQty);
                        if (ProductsCount <= 0)
                        {
                            //prompt for price start here
                            if (product.PromptForPrice && !product.PromptForMetaData)
                            {
                                bool valid = PromptForPriceDetails(sale, product, isdynamic);
                                if (valid == false)
                                    return null;
                            }
                            // Product Details for metadata start here
                            else if (product.PromptForMetaData)
                            {
                                bool valid = PromptForMetadataDetails(sale, product);
                                if (valid == false)
                                    return null;
                            }
                            else if (product.UseWeightPricing)
                            {
                                bool valid = PromptForPriceOnWeight(sale, product);
                                if (valid == false)
                                    return null;
                            }
                            else
                            {
                                if (product.PriceInterval != null)
                                {
                                    decimal _storMenuPrice = sale.MenuPrice;
                                    sale.MenuPrice = product.Price;
                                    sale.DiscountPrice = _storMenuPrice;
                                }
                                else
                                {
                                    var invenfirst = inv.Where(p => p.InvoiceId == null && p.ReceivedQty == 0).First();
                                    sale.MenuPrice = invenfirst.MRPPerUnit;
                                    sale.DiscountPrice = invenfirst.DiscountPricePerUnit > 0 ? (((100 - invenfirst.DiscountPricePerUnit) / 100) * invenfirst.MRPPerUnit) : invenfirst.MRPPerUnit;
                                    sale.AdjustPrice = invenfirst.DiscountPricePerUnit > 0 ? (((100 - invenfirst.DiscountPricePerUnit) / 100) * invenfirst.MRPPerUnit) : invenfirst.MRPPerUnit;
                                }
                            }
                        }
                        else
                        {
                            if (inv != null)
                            {
                                //prompt for price start here
                                if (product.PromptForPrice && !product.PromptForMetaData)
                                {
                                    bool valid = PromptForPriceDetails(sale, product, isdynamic);
                                    if (valid == false)
                                        return null;
                                }
                                // Product Details for metadata start here
                                else if (product.PromptForMetaData)
                                {
                                    bool valid = PromptForMetadataDetails(sale, product);
                                    if (valid == false)
                                        return null;
                                }
                                else if (product.UseWeightPricing)
                                {
                                    bool valid = PromptForPriceOnWeight(sale, product);
                                    if (valid == false)
                                        return null;
                                }
                                else
                                {
                                    var invenfirst = inv.Where(p => p.OnHandQty > 0).First();
                                    sale.MenuPrice = invenfirst.MRPPerUnit;
                                    sale.AdjustPrice = sale.DiscountPrice = invenfirst.DiscountPricePerUnit > 0 ? (((100 - invenfirst.DiscountPricePerUnit) / 100) * invenfirst.MRPPerUnit) : invenfirst.MRPPerUnit;
                                }
                            }
                        }
                    }

                    else
                    {
                        Bandaid.DialogService.ShowMessage("This product is not available in Inventory.");
                        return null;
                    }
                }
                if (product.IsAgeRestrict)
                {
                    var data = Onepos.Pos.Core.Bandaid.SessionService.ActiveSession.SelectedOrder.Customer;
                    //Onepos.Pos.Core.Bandaid.SessionService.ActiveSession.SelectedOrder.verificationRequired = true;
                    if (data == null && Onepos.Pos.Core.Bandaid.SessionService.ActiveSession.SelectedOrder.verificationRequired == false)
                    {
                        var db = new Onepos.Pos.Wpf.Controls.ListBox.Customer.CustomerBox();
                        Onepos.Pos.Core.Bandaid.DialogService.DisplayControl(db);
                    }
                    else
                    {
                        Onepos.Pos.Core.Bandaid.SessionService.ActiveSession.SelectedOrder.count = 0;
                    }
                }

                if ((Onepos.Pos.Core.Bandaid.SessionService.ActiveSession.SelectedOrder.count != -1 && product.IsAgeRestrict) || product.IsAgeRestrict == false)
                {
                    //inv.Where(p=>p.OnHandQty>0).GroupBy(p=>p.MRPPerUnit).Count()


                    if (inv.GroupBy(p => p.MRPPerUnit).Count() > 1 && product.QsrName != "Edit")

                        try
                        {
                            const string message = "Please select Price for Product";
                            Bandaid.EventAggregator.GetEvent<BusyIndicatorRequested>().Publish(true);
                            var InvenPrices = inv.GroupBy(p => p.MRPPerUnit).Select(p => p.FirstOrDefault());
                            var PriceList = InvenPrices.Select(e => e.MRPPerUnit.ToString("#.##")).ToList();
                            Bandaid.EventAggregator.GetEvent<BusyIndicatorRequested>().Publish(false);

                            ////  If morethan one price showing popup to select price option to cashier
                            ////var mb = new MessageBoxStringChoices(message, PriceList) { TextBlockCompName = { FontSize = 17.0 } };


                            ////Bandaid.DialogService.DisplayControl(mb);
                            ////if (mb.Response.Response == QuestionResponse.Yes && !string.IsNullOrEmpty(mb.Response.Value))
                            ////{
                            ////    string type = mb.Response.Value;
                            ////    sale.DiscountPrice = Convert.ToDecimal(type);

                            ////    var invenrecord = inv.Where(p => p.MRPPerUnit == sale.DiscountPrice && p.OnHandQty > 0).First();
                            ////    sale.MenuPrice = invenrecord.DiscountPricePerUnit > 0 ? (((100 - invenrecord.DiscountPricePerUnit) / 100) * sale.MenuPrice) : sale.MenuPrice;
                            ////}


                            if (PriceList.Count() > 1)
                            {
                                var price = InvenPrices.Where(x => x.MRPPerUnit != 0.00M).Select(x => x.MRPPerUnit).FirstOrDefault();
                                sale.AdjustPrice = sale.DiscountPrice = Convert.ToDecimal(price);

                                var invenrecord = inv.Where(p => p.MRPPerUnit == sale.DiscountPrice && p.OnHandQty > 0).First();
                                sale.MenuPrice = invenrecord.DiscountPricePerUnit > 0 ? (((100 - invenrecord.DiscountPricePerUnit) / 100) * sale.MenuPrice) : sale.MenuPrice;
                            }
                            else
                            {
                                sale = null;
                                return null;
                            }
                        }
                        catch (Exception ex)
                        {
                            Company.Portable.Core.Static.FileLogger.QueueBootMessage(ex.ToString(), HardCoded.BootupLogFileName);
                            Bandaid.EventAggregator.GetEvent<BusyIndicatorRequested>().Publish(false);
                            //  ExceptionService.Log(ex);
                        }
                    //  sale.Model.Store = SessionDto.StoreId;
                    ////if(seat.Sales.Count >=3)
                    ////{
                    ////   // sale.MenuPrice = 0;
                    ////    sale.DiscountPrice = 0;
                    ////}
                    //if (sale.AdjustPrice != sale.MenuPrice)
                    //{
                    //    var sale1 = seat.Sales.First();
                    //    var discount = sale1.DiscountClones.First();
                    //    //discount.Sale.Id = sale.Model.Id;
                    //    //sale.Model.DiscountClones.Add(discount);
                    //    //sale.DiscountClones.Add(discount);
                    //    // Bandaid.SessionService.ActiveSession.PosModel.DiscountClones.Add(discount);
                    //    // sale.DiscountClones.Add(sale1.DiscountClones.First());

                    //    var discountClone = new DiscountClone();
                    //    discountClone = sale1.DiscountClones.First();
                    //    sale.OnHold = false;
                    //    sale.NoteForPrint = "";

                    //    sale.DiscountClones.Add(discountClone);
                    //    discountClone.Sale = sale.Model;
                    //    Bandaid.EventAggregator.GetEvent<UpdateCheckTotals>().Publish(0);
                    //    DisplayController.UpdateMenuBoardDependencyInjections();

                    //}
                   // seat.Sales.Add(sale);

                    if (Onepos.Pos.Core.Bandaid.SessionService.ActiveSession.SelectedOrder.count > 0 && product.IsAgeRestrict)
                    {
                        Onepos.Pos.Core.Bandaid.SessionService.ActiveSession.SelectedOrder.count = -1;
                    }

                    Bandaid.SessionService.ActiveSession.PosModel.Sales.Add(sale.Model);
                    DisplayController.MenuBoardView.SelectSale(sale);
                    DisplayController.UpdateState(ApplicationCommand.CmdUpdateMenuBoardDependencyInjections);

                    if (Bandaid.SessionService.ActiveSession.SelectedOrder.ChecksPresentationContainer.Count == 0)
                    {
                        var container = new CheckContainerDto(Bandaid.SessionService.ActiveSession.SelectedOrder) { SeatNo = 1 };
                        container.Sales.Add(sale);
                        Bandaid.SessionService.ActiveSession.SelectedOrder.ChecksPresentationContainer.Add(container);
                    }
                    else
                    {
                        Bandaid.SessionService.ActiveSession.SelectedOrder.ChecksPresentationContainer.First()
                            .Sales.Add(sale);
                    }
                    DisplayController.MenuBoardView.ScrollToLastSoldSale();
                    //Publish the event that a sale was added.
                    Bandaid.EventAggregator.GetEvent<OnSalesAddedToOrder>().Publish(seat);



                    // var ctx = Onepos.Pos.Core.Bandaid.SessionService.ActiveSession.PosModel;
                    //  ctx.SaveChanges();


                    DisplayController.MenuBoardView.EnterCheckMode(1);
                    //DisplayController.MenuBoardView.TicketControlPanelSelectSalesContainer(Bandaid.SessionService.ActiveSession.SelectedOrder.SalesContainerCollection.First());
                    PoleDisplayLogic.UpdatePoleDisplayTotals(order);


                    if (!Portable.Pos.Core.Bandaid.ConfigurationService.HasCustomerFacingDisplay) return sale;

                    PoleDisplayLogic.UpdatePoleDisplay(sale.Model, sale.Model.CountOrdered, Manager.Action.Sale);
                    //PoleDisplayLogic.UpdatePoleDisplayTotals(order);

                }

                return sale;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Barcode scanning issue");
                Company.Portable.Core.Static.FileLogger.QueueBootMessage(ex.ToString(), HardCoded.BootupLogFileName);
                return null;
            }
            //}
            //else
            //{
            //    Onepos.Pos.Core.Bandaid.SessionService.ActiveSession.SelectedOrder.count -= 1;
            //}


        }

        /// <summary>
        ///     Handles the sale of a forced modifier and attaches it to another sale.
        /// </summary>
        /// <param name="mod"></param>
        private static void AddSale(ForcedModifierDto mod)
        {
            if (DisplayController.MenuBoardView.SelectedSalesContainer == null) return;


            if (mod.RemoveAtZeroCount && !DecrementProductCount(mod))
            {
                Bandaid.DialogService.ShowMessage("This product has been sold out.");
                return;
            }

            if (mod.PromptForPrice)
            {
                var mb = new MessageBoxCashMoney(0, CashBoxType.PayIn);
                Bandaid.DialogService.DisplayControl(mb);
                if (mb.Response != QuestionResponse.Yes || !(mb.TotalAmount > 0)) return;
                mod.Price = mb.TotalAmount;
                mod.ActualPrice = mb.TotalAmount;
            }
            else if (mod.PriceByWeight)
            {
                bool didRead;
                var totalPrice = Scale.GetScalePrice(mod, out didRead);
                if (!didRead) return;
                mod.ActualPrice = totalPrice;
                mod.Price = totalPrice;
            }

            DisplayController.MenuBoardView.AddForcedModifier(mod);

            //When a forced modifier is added, it is placed in a list within a list.  So to assure that
            //the ticket is showing the full view of the product and its modifiers, we must force scroll
            //to the last sale in the outer list.
            DisplayController.MenuBoardView.ScrollToLastSoldSale();
        }

        private static void AddSale(ExceptionModiferDto mod)
        {
            if (!DisplayController.MenuBoardView.HasSelectedSales()) return;

            if (mod.RemoveAtZeroCount && !DecrementProductCount(mod))
            {
                Bandaid.DialogService.ShowMessage("This product has been sold out.");
                return;
            }

            DisplayController.MenuBoardView.AddExceptionModifier(mod);

            //When a forced modifier is added, it is placed in a list within a list.  So to assure that
            //the ticket is showing the full view of the product and its modifiers, we must force scroll
            //to the last sale in the outer list.
            DisplayController.MenuBoardView.ScrollToLastSoldSale();
        }

        /// <summary>
        ///     Removes a forced modifier sale from an order that contains a specified ID.
        /// </summary>
        /// <param name="forcedMod"></param>
        private static void RemoveSale(ForcedModifierDto forcedMod)
        {
            if (DisplayController.MenuBoardView.SelectedSale == null) return;
            var fmodSale =
                DisplayController.MenuBoardView.GetModifier(s => s.ProductId == forcedMod.Id && s.IsForcedModifier);
            if (fmodSale != null)
            {
                DisplayController.MenuBoardView.RemoveModifier(fmodSale);
            }
            DisplayController.MenuBoardView.SelectedSalesContainer.OnPropertyChanged("SalesGroupedBySimilar");
        }

        /// <summary>
        ///     When a product group, exception modifier group or forced modifier group is clicked,
        ///     this function will dispath the correct action based upon the passed in object type.
        /// </summary>
        /// <param name="productGroup"></param>
        private static void SelectGroup(object productGroup)
        {
            try
            {
                if (productGroup == null) return;
                //A product group was clicked.
                if (productGroup.GetType() == typeof(ProductGroupDto))
                {
                    var pg = productGroup as ProductGroupDto;

                    //Don't waste CPU cycles if it is already the current selection.
                    if (pg != null && !pg.IsSelected)
                    {

                        pg.IsSelected = true;
                        DisplayController.MenuBoardView.SetProductsItemsSource(MenuLogic.GetProductsAsNoTracking(pg.Id,
                            Bandaid
                                .SessionService
                                .ActiveSession
                                .IsTrainingMode));
                    }
                }
                //An exception modifier gruop was clicked.
                else if (productGroup.GetType() == typeof(ExceptionModiferGroupDto))
                {
                    var eg = productGroup as ExceptionModiferGroupDto;
                    //Don't waste CPU cycles if it is already the current selection.
                    if (eg != null && !eg.IsSelected)
                    {
                        eg.IsSelected = true;
                        DisplayController.MenuBoardView.SetProductsItemsSource(
                            MenuLogic.GetExceptionsAsNoTracking(eg.Id,
                                Bandaid.SessionService.ActiveSession.IsTrainingMode));
                    }
                }
                //A forced modifier group was clicked.
                else if (productGroup.GetType() == typeof(ForcedModifierGroupDto))
                {
                    var fg = productGroup as ForcedModifierGroupDto;

                    //Don't waste CPU cycles if it is already the current selection.
                    if (fg != null && !fg.IsSelected)
                    {
                        fg.IsSelected = true;
                        DisplayController.MenuBoardView.SetProductsItemsSource(fg.ForcedModifiers);
                    }
                }
            }
            catch (Exception ex)
            {
                Portable.Pos.Core.Bandaid.ExceptionService.Log(ex);
                Company.Portable.Core.Static.FileLogger.QueueBootMessage(ex.ToString(), HardCoded.BootupLogFileName);
            }
        }

        /// <summary>
        ///     Whenever a product, exception modifier or forced modifier is clicked, the assocatied UI controll will call down
        ///     into this function with a references to the DTO assocatiated with the object type.
        /// </summary>
        /// <param name="p"></param>
        private static void SelectProduct(object p)
        {
            var order = Bandaid.SessionService.ActiveSession.SelectedOrder;
            if (order == null) return;
            if (order.IsClosed == true)
            {
                Bandaid.DialogService.ShowMessage("order is closed, Adding a product create new order");
                DisplayController.MenuBoardView.EnterCheckMode(1);
                return;
            }

            if (!Common.CanReopenOrder(order,
                "This order is closed.  Adding a product to this order will reopen it.  Would you like to reopen this order?")) return;

            //At this point we know what product has been selected in the ListBox.  Set the SelectedItem to null that way the same
            //item can be selected back to back.  For example, if you press pickles and then decided to press pickles again to
            //remove pickles, in a normal listbox, the SelectionChanged event will not fire because, well the selection has not changed.
            //If we force it to null, then in fact when it is clicked again, it will change.
            DisplayController.MenuBoardView.SetSelectedProduct(null);
            //<---- This one line of code can make your head hurt if you remove it!!!!

            try
            {
                //A product was selected.
                if (p.GetType() == typeof(ProductDto))
                {
                    var product = p as ProductDto;

                    //If the user has deleted all of their seats and presses a product button, we need to assure we auto create a seat
                    //for them.
                    if (order.SeatsPresentationContainer.Count == 0)
                    {
                        DisplayController.MenuBoardView.TicketControlAddContainer();
                    }

                    //Add the selected product as a sale.
                    var sale = AddSale(product, null);

                    //See if there are any forced modifier groups and attempt to add them to the display if there are.
                    if (product != null && sale != null)
                    {
                        var groups = MenuLogic.GetForcedModGroupsAsNoTracking(product.Id,
                            Bandaid.SessionService.ActiveSession.IsTrainingMode);
                        if (groups.Count != 0)
                        {
                            UpdateProductsDisplay(groups, true);
                        }
                    }
                }
                //An exception modifier was selected.
                else if (p.GetType() == typeof(ExceptionModiferDto))
                {
                    var exceptionMod = p as ExceptionModiferDto;
                    AddSale(exceptionMod);
                }
                //A forced modifier was selected.
                else if (p.GetType() == typeof(ForcedModifierDto))
                {
                    //A forced modifier was just selected.
                    var fm = p as ForcedModifierDto;

                    //Grab the forced modifier group that is currently selected.
                    var fg =
                        DisplayController.MenuBoardView.GetProductGroupDto() as
                            ForcedModifierGroupDto;

                    if (fg != null && fm != null)
                    {
                        //The forced modifier has a flag that indicates if it has or has not been selected.  This is used to allow toggling between turning the
                        //forced modifier on and off.  So if it was already on, we are removing it and likewise if it was off we are adding it.
                        var isAdd = !fm.IsSelected;

                        //If we are removing a forced modifier or we are adding it and the user has not reached the max count yet.
                        if (fg.MaximumChoice == 0)
                        {
                            Bandaid.DialogService.ShowMessage("The configuration for " + fg.Name +
                                                              " has a max choice set to 0 choices.  Please increase this value in the forced modifier group configuration.");
                        }
                        else if (!isAdd || fg.SelectedCount < fg.MaximumChoice)
                        {
                            //Invert the flag.
                            fm.IsSelected = !fm.IsSelected;

                            //If we are adding this forced modifier
                            if (isAdd)
                            {
                                //Increase the selected count on the product group.
                                fg.SelectedCount++;
                                AddSale(fm);

                                //If we have reached the maximum choice for this forced modifier group we
                                //need to switch to one of the other groups, or exit out of forced modifier 
                                //group mode.  It all depends on if there are more to add or not.
                                if (fg.SelectedCount == fg.MaximumChoice)
                                {
                                    //The product and product group buttons as well as most all buttons in this application are built using
                                    //templates in the resource dictionary from XAML.  This cuts down on a tremendous amount of code, but
                                    //makes it a little tricky when it comes to ListBoxes.  We have the data, we have the listbox, but we
                                    //don't have the button that is located in the listbox.  This function will walk the visual tree and
                                    //search for all buttons on the product groups panel that are of type VanillaButton (a generic button class).
                                    //What makes it more challenging is that this method only works if we know the ListBox has already created
                                    //all of the templated content.  In this case since we are in forced modifier mode, we know the buttons
                                    //exist.
                                    var allProductGroupButtons =
                                        VisualTreeHelpers.FindVisualChildren<VanillaButton>(
                                            DisplayController.MenuBoardView.ListBoxProductGroups);

                                    //Find the next forced modifier group that needs to have products selected from and select it.  If we don't find
                                    //it, then we will essentially exit out of forced modifier mode.
                                    var found = false;
                                    foreach (var button in allProductGroupButtons)
                                    {
                                        var g = button.DataContext as ForcedModifierGroupDto;
                                        if (g != null && g.SelectedCount >= g.MaximumChoice) continue;
                                        //We found a forced modifier group that still has modifiers that need to be selected.
                                        //Invoke a Click on the button to save the user a step.  BeginInvoke means it will likely happen
                                        //after we exit this function which is good.
                                        var localbutton = button;
                                        Application.Current.Dispatcher.BeginInvoke(
                                            new Action(localbutton.ClickMe), null);
                                        found = true;
                                        break;
                                    }

                                    //Did not find a group that needs forced mods selected from.  Exit out of this mode back into product entry mode.
                                    if (!found)
                                    {
                                        DisplayController.UpdateState(ApplicationCommand.CmdSelectMenuBoardScreen);
                                    }
                                }
                            }
                            else
                            {
                                //In this case, the user clicked on a forced modifier that was already added.  This will remove it and will also
                                //take the highlighting off of the button due to the template bindings in the resources dictionary.
                                fm.IsSelected = false;
                                fg.SelectedCount--;
                                if (fg.SelectedCount < 0) fg.SelectedCount = 0;
                                RemoveSale(fm);
                            }
                        }
                    }
                }

               
            }
            catch (Exception ex)
            {
                // Portable.Pos.Core.Bandaid.ExceptionService.Log(ex);
                Company.Portable.Core.Static.FileLogger.QueueBootMessage(ex.ToString(), HardCoded.BootupLogFileName);
            }
        }

        /// <summary>
        ///     This function will assure that the first product group has been selected.
        /// </summary>
        public static void SelectFirstMenuItem()
        {
            var ctx = DisplayController.MenuBoardView.ListBoxProductGroups.ItemsSource as IEnumerable<object>;

            // ReSharper disable PossibleMultipleEnumeration
            if (ctx == null || !ctx.Any()) return;
            SelectGroup(ctx.First());
        }

        /// <summary>
        ///     This function should be called anytime the MenuBoard page is entered or we are exiting out of forced modifier or
        ///     exception modifier mode and want to enter back into product entry mode.  It is an async method.
        ///     CAUTION: Do not call this function directly.  Instead
        /// </summary>
        public static async void OnShowMenuBoard()
        {
            try
            {
                DisplayController.MenuBoardView.TicketControlUnselectAll();

                //Get the product groups.
                var getGroupsTask =
                    MenuLogic.GetProductGroupsAsNoTrackingAsync(Bandaid.SessionService.ActiveSession.IsTrainingMode);
                var groups = await getGroupsTask;
                UpdateProductsDisplay(groups, lockDisplay: false);
                //DisplayController.MenuBoardView.ScrollProductGroupIntoView(groups[0]);
            }
            catch (Exception e)
            {
                Company.Portable.Core.Static.FileLogger.QueueBootMessage(e.ToString(), HardCoded.BootupLogFileName);
                //  Portable.Pos.Core.Bandaid.ExceptionService.Log(e);
            }
        }


        /// <summary>
        ///     When the display controller cannot handle a task it will call back down into this function where it can be handled
        ///     in the
        ///     logic of the application code.
        /// </summary>
        /// <param name="cmd"></param>
        public static void ProgramStateChange(ApplicationCommand cmd)
        {
            try
            {
                if (cmd == ApplicationCommand.CmdEnterExceptionModifierMode)
                {
                    if (DisplayController.MenuBoardView.HasSelectedSales())
                    {
                        DisplayController.IsExceptionModifierMode = true;
                        ObservableCollection<object> groups = null;

                        //If the selected count is one, then get the exception modifier group
                        //for that product.  If that product does not have an exception modifier
                        //group then return an empty collection. 
                        //
                        //If the selected count is more than one, then assure that they all belong to the
                        //same product group.
                        var sale = DisplayController.MenuBoardView.SelectedSalesList().FirstOrDefault();

                        if (sale != null)
                        {
                            groups =
                                MenuLogic.GetProductGroupExceptionModifierGroupsAsNoTracking(
                                    DisplayController.MenuBoardView.SelectedSalesList().First()
                                        .ProductGroupId,
                                    Bandaid.SessionService.ActiveSession.IsTrainingMode);
                        }

                        UpdateProductsDisplay(groups ?? new ObservableCollection<object>(), lockDisplay: false);
                    }
                }
                else if (cmd != ApplicationCommand.CmdInvalidPassword)
                {
                    var message = Language.GetMessage(cmd);
                    Bandaid.DialogService.ShowMessage(message);
                }
            }
            catch (Exception e)
            {
                Company.Portable.Core.Static.FileLogger.QueueBootMessage(e.ToString(), HardCoded.BootupLogFileName);
                //  Portable.Pos.Core.Bandaid.ExceptionService.Log(e);
            }
        }

        /// <summary>
        ///     This function will assure that the groups are displayed in the groups panel and the products are displayed in the
        ///     products panel.
        ///     This will work for products and modifiers both.
        /// </summary>
        /// <param name="groups"></param>
        /// <param name="lockDisplay"></param>
        private static void UpdateProductsDisplay(IEnumerable<object> groups, bool lockDisplay)
        {
            if (groups != null)
            {
                //Set the groups in the groups panel.
                DisplayController.MenuBoardView.SetProductGroupsItemsSource(groups);

                //Clear out the products that were on the products panel.
                DisplayController.MenuBoardView.SetProductsItemsSource(null);

                //Make sure that there are items in the list
                var ic = DisplayController.MenuBoardView.ListBoxProductGroups.Items;
                if (ic != null && ic.Count > 0)
                {
                    //Grab the first item.
                    var o = ic[0];

                    //Set the selected index to the first group so that the first group button will highlight due to
                    //databinding and the templates in the resources file.
                    DisplayController.MenuBoardView.ListBoxProductGroups.SelectedIndex = 0;

                    //Now call select group to show all of the products for the selected default group.
                    SelectGroup(o);
                }
                else
                {
                    if (DisplayController.IsExceptionModifierMode)
                    {
                        //During this case, the user entered into exception modifier mode, but there were
                        //no exception modifier groups.  Select the Special Modifier Button Instead.
                        DisplayController.MenuBoardView.ClickModifiersButton();
                    }
                }
            }
            //This will prevent the user from using any buttons that cannot be used while in the
            //current state.
            DisplayController.UpdateMenuBoardDependencyInjections(isPendingAction: lockDisplay);
        }
    }


}