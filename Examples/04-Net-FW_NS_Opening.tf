############################### Paloalto NS Firewall rules #########################################
locals {
  NSContracts = {

    PROD_DC1 = {
      NS_FW_Rules_01 = {
        json_file_name = "NS_FW_Rules_SECINFRA_DC1.json" // Fill the good json file name
      }
    }

    PROD_DC2 = {
      NS_FW_Rules_01 = {
        json_file_name = "NS_FW_Rules_SECINFRA_DC2.json" // Fill the good json file name
      }
    }
  }
}

// Module documentation: https://wiki.example.com/modules/ns-contract
module "NSContract" {
  source         = "git::https://gitlab.internal.example.com/infra/terraform/modules/iaasv2/iaasv2-nscontract.git?ref=1.0"
  for_each       = local.NSContracts[terraform.workspace]
  name           = each.key
  object_code    = var.object_code
  json_file_name = each.value.json_file_name
  depends_on     = [module.EPG]
}
###########################################################################################

