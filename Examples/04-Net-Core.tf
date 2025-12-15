###### CAUTION requirements - Terraform User Guide: https://wiki.example.com/terraform-guide ######

################### CISCO ACI TENANT CREATION ###################
locals {
  tenantaci = {

    PROD_DC1 = {
      Tenant_ACI = {
        fabric      = "I2A-DC1"
        subzone     = "XS4-PRD"
        description = "SECINFRA - Prod DC1"
      }
    }

    PROD_DC2 = {
      Tenant_ACI = {
        fabric      = "I2A-DC2"
        subzone     = "XS4-PRD"
        description = "SECINFRA - Prod DC2"
      }

    }

  }
}

// Module documentation: https://wiki.example.com/modules/tenant-aci
module "Tenant" {
  source             = "git::https://gitlab.internal.example.com/infra/terraform/modules/iaasv2/iaasv2-tenantaci.git?ref=1.1"
  for_each           = local.tenantaci[terraform.workspace]
  object_code        = var.object_code
  fabric             = each.value.fabric
  subzone            = each.value.subzone
  tenant_description = each.value.description
}

################ CISCO ACI BRIDGE DOMAIN CREATION #####################
locals {
  bdaci = {

    PROD_DC1 = {
      COMMON = {
        object_code = "Tenant_ACI"
        subnets = jsonencode(
          [
            "${local.address_space};${try(cidrhost(local.address_space, 1), "")}"
          ]
        )
        scope = jsonencode(
          [
            "public",
            "shared"
          ]
        )
      }
    }

    PROD_DC2 = {
      COMMON = {
        object_code = "Tenant_ACI"
        subnets = jsonencode(
          [
            "${local.address_space};${try(cidrhost(local.address_space, 1), "")}"
          ]
        )
        scope = jsonencode(
          [
            "public",
            "shared"
          ]
        )
      }
    }

  }
}

// Module documentation: https://wiki.example.com/modules/bd-aci
module "BDACI" {
  source      = "git::https://gitlab.internal.example.com/infra/terraform/modules/iaasv2/iaasv2-bdaci.git?ref=1.5"
  for_each    = local.bdaci[terraform.workspace]
  bd_name     = each.key
  object_code = try(module.Tenant[each.value.object_code].output_object_code, var.object_code)
  subnets     = each.value.subnets
  scope       = try(each.value.scope, "[]")
}

##################### EPG CREATION #################################
locals {
  epgs = {

    PROD_DC1 = {
      SECINFRA_GW = {
        bd_name = "COMMON"
      }
      SECINFRA_SERVICES = {
        bd_name = "COMMON"
      }
      SECINFRA_AD_LDAP = {
        bd_name = "COMMON"
      }
      SECINFRA_SECU_REPO = {
        bd_name = "COMMON"
      }
      SECINFRA_EXCHANGE = {
        bd_name = "COMMON"
      }
    }

    PROD_DC2 = {

      SECINFRA_GW = {
        bd_name = "COMMON"
      }
      SECINFRA_SERVICES = {
        bd_name = "COMMON"
      }
      SECINFRA_AD_LDAP = {
        bd_name = "COMMON"
      }
      SECINFRA_SECU_REPO = {
        bd_name = "COMMON"
      }
      SECINFRA_EXCHANGE = {
        bd_name = "COMMON"
      }
    }

  }
}

// Module documentation: https://wiki.example.com/modules/epg
module "EPG" {
  source         = "git::https://gitlab.internal.example.com/infra/terraform/modules/iaasv2/iaasv2-epg.git?ref=1.1"
  for_each       = local.epgs[terraform.workspace]
  epg_name       = each.key
  object_code    = var.object_code
  bd_name        = module.BDACI[each.value.bd_name].output_bd_name
  vmw_name       = try(each.value.vmw_name, "_default_")
  vmw_resolution = try(each.value.vmw_resolution, "pre-provision")
  bm_name        = try(each.value.bm_name, "")
  bm_resolution  = try(each.value.bm_resolution, "immediate")
}

##################### StaticPort CREATION #################################
locals {
  staticports = {

    PROD_DC1 = {}

    PROD_DC2 = {}

  }
}

// Module documentation: https://wiki.example.com/modules/staticport
module "StaticPort" {
  source         = "git::https://gitlab.internal.example.com/infra/terraform/modules/iaasv2/iaasv2-staticport.git?ref=1.0"
  for_each       = local.staticports[terraform.workspace]
  object_code    = var.object_code
  name           = each.key
  epg_name       = module.EPG[each.value.epg_name].output_epg_name
  interface      = each.value.interface
  interface_mode = each.value.interface_mode
  vlan           = each.value.vlan
  node           = each.value.node
}
