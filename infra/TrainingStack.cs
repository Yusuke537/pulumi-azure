using Pulumi;
using Pulumi.Azure.AppService;
using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.Core;
using Pulumi.Azure.Storage;

// ReSharper disable once CheckNamespace
public class TrainingStack : Stack
{
    public TrainingStack() : base(new StackOptions
    {
        ResourceTransformations =
        {
            args =>
            {
                var tagp = args.Args.GetType().GetProperty("Tags");
                if (tagp is null)
                {
                    return null;
                }
                var tags = (InputMap<string>)tagp.GetValue(args.Args, null)!;
                tags["ProvisionedBy"] = "Pulumi";
             
                tagp.SetValue(args.Args, tags, null);
                return new ResourceTransformationResult(args.Args, args.Options);
            }
        }
    })
    {
        var config = new Config();
        var suffix = config.Get("suffix");

        var resourceGroup = new ResourceGroup("rg-xcc-pulumi");

        var plan = new Plan("sp-xcc-dynamic", new PlanArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Kind = "FunctionApp",
            Sku = new PlanSkuArgs
            {
                Tier = "Dynamic",
                Size = "Y1"
            }
        });

        var storageAccount = new Account("funcxccpulumi", new AccountArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AccountReplicationType = "LRS",
            AccountTier = "Standard",
            AccountKind = "StorageV2"
        });
         
        var app = new FunctionApp($"func-{suffix}-app", new FunctionAppArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AppServicePlanId = plan.Id,
            StorageAccountName = storageAccount.Name,
            StorageAccountAccessKey = storageAccount.PrimaryAccessKey,
            Version = "~3",
            SiteConfig = new FunctionAppSiteConfigArgs
            {
                Http2Enabled = true,
                ScmType = "VSTSRM"
            },
            AppSettings = new InputMap<string>
            {
                { "WEBSITE_RUN_FROM_PACKAGE", "1"}
            }
        });

        var stagingSlot = new FunctionAppSlot("staging", new FunctionAppSlotArgs
        {
            Name = "staging", // do not let Pulumi append random suffix
            ResourceGroupName = resourceGroup.Name,
            AppServicePlanId = plan.Id,
            StorageAccountName = storageAccount.Name,
            StorageAccountAccessKey = storageAccount.PrimaryAccessKey,
            FunctionAppName = app.Name,
            Version = "~3",
            SiteConfig = new FunctionAppSlotSiteConfigArgs
            {
                Http2Enabled = true,
                ScmType = "VSTSRM",
            },
            AppSettings = new InputMap<string>
            {
                { "WEBSITE_RUN_FROM_PACKAGE", "1"}
            }
        });
        
        //init outputs
        ResourceGroupName = resourceGroup.Name;
        FunctionStatingSlotName = stagingSlot.Name;
        AppServicePlanId = app.AppServicePlanId;
        FunctionAppName = app.Name;
        StagingSlotUri = stagingSlot.DefaultHostname;
    }
    
    [Output]
    public Output<string> ResourceGroupName { get; set; }

    [Output]
    public Output<string> AppServicePlanId { get; set; }
    
    [Output]
    public  Output<string> FunctionAppName { get; set; }

    [Output]
    public Output<string> FunctionStatingSlotName { get; set; }

    [Output] 
    public Output<string> StagingSlotUri { get; set; }
}
