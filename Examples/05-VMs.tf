locals {
  vms = {

    PROD_DC1 = {
      gw-win-01p = {
        os            = "windows2022",
        size          = "4vCPU / 8GB RAM",
        securityGroup = "SECINFRA_GW",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      gw-lin-01p = {
        os            = "rhel9_9.5",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_GW",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      svc-lin-01p = {
        os            = "rhel9_9.5",
        size          = "4vCPU / 8GB RAM",
        securityGroup = "SECINFRA_SERVICES",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      svc-win-01p = {
        os            = "windows2022",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_SERVICES",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      ldap-lin-01p = {
        os            = "rhel9_9.5",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_AD_LDAP",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      dc-win-01p = {
        os            = "windows2022",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_AD_LDAP",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      dc-win-02p = {
        os            = "windows2022",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_AD_LDAP",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      proxy-lin-01p = {
        os            = "rhel9_9.5",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_EXCHANGE",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      proxy-lin-02p = {
        os            = "rhel9_9.5",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_EXCHANGE",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      }
    }

    PROD_DC2 = {
      gw-win-01d = {
        os            = "windows2022",
        size          = "4vCPU / 8GB RAM",
        securityGroup = "SECINFRA_GW",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      gw-lin-01d = {
        os            = "rhel9_9.5",
        size          = "2vCPU / 2GB RAM",
        securityGroup = "SECINFRA_GW",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      ldap-lin-01d = {
        os            = "rhel9_9.5",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_AD_LDAP",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      ldap-lin-02d = {
        os            = "rhel9_9.5",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_AD_LDAP",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      dc-win-01d = {
        os            = "windows2022",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_AD_LDAP",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      dc-win-02d = {
        os            = "windows2022",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_AD_LDAP",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      dc-win-03d = {
        os            = "windows2022",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_AD_LDAP",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      dc-win-04d = {
        os            = "windows2022",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_AD_LDAP",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      svc-win-01d = {
        os            = "windows2022",
        size          = "2vCPU / 2GB RAM",
        securityGroup = "SECINFRA_SERVICES",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      svc-lin-01d = {
        os            = "rhel9_9.5",
        size          = "4vCPU / 8GB RAM",
        securityGroup = "SECINFRA_SERVICES",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      svc-win-02d = {
        os            = "windows2022",
        size          = "4vCPU / 16GB RAM",
        securityGroup = "SECINFRA_SERVICES",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      svc-win-03d = {
        os            = "windows2022",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_SERVICES",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      file-win-01d = {
        os            = "windows2022",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_SERVICES",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      svc-lin-02d = {
        os            = "rhel9_9.5",
        size          = "4vCPU / 32GB RAM",
        securityGroup = "SECINFRA_SERVICES",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      sec-win-01d = {
        os            = "windows2022",
        size          = "4vCPU / 16GB RAM",
        securityGroup = "SECINFRA_SECU_REPO",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      sec-lin-01d = {
        os            = "rhel8_8.9",
        size          = "8vCPU / 16GB RAM",
        securityGroup = "SECINFRA_SECU_REPO",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      }
      proxy-lin-01d = {
        os            = "rhel9_9.5",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_EXCHANGE",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      proxy-lin-02d = {
        os            = "rhel9_9.5",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_EXCHANGE",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      },
      proxy-lin-03d = {
        os            = "rhel9_9.5",
        size          = "2vCPU / 4GB RAM",
        securityGroup = "SECINFRA_EXCHANGE",
        bp_version    = "4.7.2",
        netClass      = "GOLD-1000Mbps",
        storClass     = "GOLD-2000IOPS",
      }
    }
  }
}

// documentation here: https://wiki.example.com/modules/create-vm
module "VM" {
  source               = "git::https://gitlab.internal.example.com/infra/terraform/modules/iaasv2-createvm.git?ref=4.7.1"
  for_each             = local.vms[terraform.workspace]
  object_code          = var.object_code
  name                 = each.key
  os                   = try(each.value.os, var.os)
  size                 = try(each.value.size, var.size)
  netClass             = try(each.value.netClass, var.netClass)
  storClass            = try(each.value.storClass, var.storClass)
  cmdbEnv              = local.cmdb_environment
  securityGroup        = module.EPG[each.value.securityGroup].output_epg_name
  catalog_item_version = try(each.value.bp_version, var.bp_version)
  SSD                  = try(each.value.SSD, var.SSD)
}
