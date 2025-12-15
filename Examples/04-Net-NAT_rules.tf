############################ Paloalto NAT rules ######################################
locals {
  NAT_rules = {

    PROD_DC1 = {
      // NAT_Rules_01 = {
      //   json_file_name = "NAT_Rules_PROD_DC1.json" // Fill the good json file name
      // }
    }

    PROD_DC2 = {
      // NAT_Rules_01 = {
      //   json_file_name = "NAT_Rules_PROD_DC2.json" // Fill the good json file name
      // }
    }

  }
}

// Module documentation: https://wiki.example.com/modules/paloalto-nat
module "NAT_rules" {
  source         = "git::https://gitlab.internal.example.com/infra/terraform/modules/iaasv2/iaasv2-paloaltonatrule.git?ref=1.0"
  for_each       = local.NAT_rules[terraform.workspace]
  name           = each.key
  object_code    = var.object_code
  json_file_name = each.value.json_file_name
}

###########################################################################################

