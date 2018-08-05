﻿using Autofac;
using Autofac.Integration.Mvc;
using AutoMapper;
using Hangfire;
using System.Linq;
using System.Web.Mvc;
using WorkFlowManager.Common.DataAccess._Context;
using WorkFlowManager.Common.DataAccess._UnitOfWork;
using WorkFlowManager.Common.DataAccess.Repositories;
using WorkFlowManager.Common.Enums;
using WorkFlowManager.Common.Tables;
using WorkFlowManager.Common.ViewModels;
using WorkFlowManager.Services.DbServices;

namespace WorkFlowManager.Web
{

    public class ContainerJobActivator : JobActivator
    {
        private IContainer _container;

        public ContainerJobActivator(IContainer container)
        {
            _container = container;
        }
    }

    public class DependencyRegistrar
    {
        public static void RegisterDependencies()
        {


            var builder = new ContainerBuilder();
            builder.RegisterControllers(typeof(MvcApplication).Assembly);
            builder.RegisterModule(new AutofacWebTypesModule());

            builder.RegisterType<UnitOfWork>()
            .As<IUnitOfWork>()
            .InstancePerLifetimeScope();


            builder.RegisterType<DataContext>()
                .As<IDbContext>()
                .InstancePerBackgroundJob()
                .InstancePerDependency();


            builder.RegisterGeneric(typeof(BaseRepository<>))
                .As(typeof(IRepository<>))
                .InstancePerDependency();



            builder.RegisterType<DecisionMethodService>()
                 .AsSelf()
                 .InstancePerLifetimeScope();

            builder.RegisterType<DocumentService>()
                .AsSelf()
                .InstancePerLifetimeScope();

            builder.RegisterType<FormService>()
                .AsSelf()
                .InstancePerLifetimeScope();


            builder.RegisterType<TestWorkFlowProcessService>()
                .AsSelf()
                .InstancePerLifetimeScope();

            builder.RegisterType<WorkFlowDataService>()
                .AsSelf()
                .InstancePerLifetimeScope();


            builder.RegisterType<WorkFlowProcessService>()
                .AsSelf()
                .InstancePerLifetimeScope();


            builder.RegisterType<WorkFlowService>()
                .AsSelf()
                .InstancePerLifetimeScope();








            //builder.RegisterType<WorkFlowService>().AsSelf().UsingConstructor(typeof(IUnitOfWork)).InstancePerLifetimeScope();



            //builder.RegisterType<DocumentService>().AsSelf().UsingConstructor(typeof(IUnitOfWork)).InstancePerLifetimeScope();



            //builder.RegisterType<FormService>().AsSelf().UsingConstructor(typeof(IUnitOfWork)).InstancePerLifetimeScope();
            //builder.RegisterType<DecisionMethodService>().AsSelf().UsingConstructor(typeof(IUnitOfWork)).InstancePerLifetimeScope();




            var container = builder.Build();

            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Process, ProcessVM>()
                    .ForMember(a => a.IsCondition, opt => opt.MapFrom(c => (c.GetType() == typeof(Condition) || c.GetType() == typeof(DecisionPoint))))
                    .ForMember(a => a.ConditionId, opt => opt.MapFrom(c => (c as ConditionOption).ConditionId));


                cfg.CreateMap<WorkFlowTrace, WorkFlowTrace>()
                .ForMember(dest => dest.ConditionOption, opt => opt.Ignore())
                .ForMember(dest => dest.Process, opt => opt.Ignore())
                .ForMember(dest => dest.Owner, opt => opt.Ignore());

                cfg.CreateMap<WorkFlowTraceForm, WorkFlowTrace>();
                cfg.CreateMap<WorkFlowTrace, WorkFlowTraceForm>();

                cfg.CreateMap<ProcessForm, Process>()
                .Include<ProcessForm, Condition>()
                .Include<ProcessForm, ConditionOption>()
                .Include<ProcessForm, DecisionPoint>()
                .ForMember(a => a.MonitoringRoleList,
                    opt => opt.MapFrom(c => c.MonitoringRoleList.Where(x => x.IsChecked == true).Select(t => new ProcessMonitoringRole
                    {
                        ProcessId = c.Id,
                        ProjectRole = t.ProjectRole
                    })));

                cfg.CreateMap<ProcessForm, Condition>();
                cfg.CreateMap<ProcessForm, ConditionOption>();
                cfg.CreateMap<ProcessForm, DecisionPoint>();

                cfg.CreateMap<DecisionMethodViewModel, DecisionMethod>();
                cfg.CreateMap<FormViewViewModel, FormView>();
                cfg.CreateMap<Process, Process>();

                cfg.CreateMap<Process, ProcessForm>()
                    .ForMember(a => a.MonitoringRoleList, opt => opt.MapFrom(c => c.MonitoringRoleList.Select(t => new MonitoringRoleCheckbox { IsChecked = true, ProjectRole = t.ProjectRole })))
                    .ForMember(a => a.ConditionId, opt => opt.MapFrom(c => (c as ConditionOption).ConditionId))
                    .ForMember(a => a.ConditionName, opt => opt.MapFrom(c => (c as ConditionOption).Condition.Name))
                    .ForMember(a => a.DecisionMethodId, opt => opt.MapFrom(c => (c as DecisionPoint).DecisionMethodId))
                    .ForMember(a => a.ProcessType, opt => opt.MapFrom(c => (c.GetType() == typeof(ConditionOption) ? ProcessType.OptionList :
                                                                                (c.GetType() == typeof(DecisionPoint) ? ProcessType.DecisionPoint :
                                                                                    (c.GetType() == typeof(Condition) ? ProcessType.Condition : ProcessType.Process)))))
                    .ForMember(a => a.RepetitionFrequenceByHour, opt => opt.MapFrom(c => (c as DecisionPoint).RepetitionFrequenceByHour))
                    .ForMember(a => a.Value, opt => opt.MapFrom(c => (c as ConditionOption).Value));


                cfg.CreateMap<TestForm, TestFormViewModel>();
                cfg.CreateMap<TestFormViewModel, TestForm>();
            }
            );


            GlobalConfiguration.Configuration.UseActivator(new ContainerJobActivator(container));
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));



        }
    }
}