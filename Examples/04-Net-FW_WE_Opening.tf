########################### FW Opening WE CREATION ###########################

locals {
  contracts = {
    PROD_DC1 = {
      Trust_AD_Local_GW_to_AD_LDAP = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_AD_LDAP"
        ports = jsonencode([
          "TCP_53", "TCP_88", "TCP_135", "TCP_139", "TCP_389", "TCP_445",
          "TCP_464", "TCP_636", "TCP_3268", "TCP_3269", "TCP_5985-5986",
          "TCP_9389", "TCP_25000-25010", "TCP_49152-65535",
          "UDP_53", "UDP_88", "UDP_123", "UDP_137", "UDP_138", "UDP_389", "UDP_464"
        ])
      }

      Trust_AD_Local_SERVICES_to_AD_LDAP = {
        epg_source      = "SECINFRA_SERVICES"
        epg_destination = "SECINFRA_AD_LDAP"
        ports = jsonencode([
          "TCP_53", "TCP_88", "TCP_135", "TCP_139", "TCP_389", "TCP_445",
          "TCP_464", "TCP_636", "TCP_3268", "TCP_3269",
          "TCP_9389", "TCP_25000-25010", "TCP_49152-65535",
          "UDP_53", "UDP_88", "UDP_123", "UDP_389", "UDP_464"
        ])
      }

      KeePass_Access_GW_to_SERVICES = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_445"])
      }

      Admin_RDP_GW_to_AD_LDAP = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_AD_LDAP"
        ports           = jsonencode(["TCP_3389", "TCP_5985-5986"])
      }

      Admin_RDP_GW_to_SERVICES = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_3389", "TCP_5985-5986", "TCP_445"])
      }
    }

    PROD_DC2 = {
      AD_Auth_SERVICES_to_AD_LDAP = {
        epg_source      = "SECINFRA_SERVICES"
        epg_destination = "SECINFRA_AD_LDAP"
        ports = jsonencode([
          "TCP_53", "TCP_88", "TCP_135", "TCP_139", "TCP_389", "TCP_445",
          "TCP_464", "TCP_636", "TCP_3268", "TCP_3269", "TCP_5140",
          "TCP_9389", "TCP_25000-25010", "TCP_49152-65535",
          "UDP_53", "UDP_88", "UDP_123", "UDP_137", "UDP_138", "UDP_389", "UDP_464"
        ])
      }

      AD_Auth_GW_to_AD_LDAP = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_AD_LDAP"
        ports = jsonencode([
          "TCP_53", "TCP_88", "TCP_135", "TCP_139", "TCP_389", "TCP_445",
          "TCP_464", "TCP_636", "TCP_3268", "TCP_3269",
          "TCP_5985-5986",
          "TCP_9389", "TCP_25000-25010", "TCP_49152-65535",
          "UDP_53", "UDP_88", "UDP_123", "UDP_389", "UDP_464"
        ])
      }

      AD_Auth_SECU_REPO_to_AD_LDAP = {
        epg_source      = "SECINFRA_SECU_REPO"
        epg_destination = "SECINFRA_AD_LDAP"
        ports = jsonencode([
          "TCP_53", "TCP_88", "TCP_135", "TCP_139", "TCP_389", "TCP_445",
          "TCP_464", "TCP_636", "TCP_3268", "TCP_3269",
          "TCP_9389", "TCP_25000-25010", "TCP_49152-65535",
          "UDP_53", "UDP_88", "UDP_123", "UDP_389", "UDP_464"
        ])
      }

      AD_Auth_EXCHANGE_to_AD_LDAP = {
        epg_source      = "SECINFRA_EXCHANGE"
        epg_destination = "SECINFRA_AD_LDAP"
        ports = jsonencode([
          "TCP_53", "TCP_88", "TCP_135", "TCP_139", "TCP_389", "TCP_445",
          "TCP_464", "TCP_636", "TCP_3268", "TCP_3269",
          "TCP_9389", "TCP_25000-25010", "TCP_49152-65535",
          "UDP_53", "UDP_88", "UDP_123", "UDP_389", "UDP_464"
        ])
      }

      Katello_Admin_GW_to_SERVICES = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_22", "TCP_443", "TCP_9090"])
      }

      Katello_Clients_GW_to_SERVICES = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_443", "TCP_8140"])
      }

      Katello_Clients_AD_LDAP_to_SERVICES = {
        epg_source      = "SECINFRA_AD_LDAP"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_443", "TCP_8140"])
      }

      Katello_Clients_SECU_REPO_to_SERVICES = {
        epg_source      = "SECINFRA_SECU_REPO"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_443", "TCP_8140"])
      }

      Katello_Clients_EXCHANGE_to_SERVICES = {
        epg_source      = "SECINFRA_EXCHANGE"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_443", "TCP_8140"])
      }

      Katello_Monitoring_GW_to_SERVICES = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["ICMP"])
      }

      Katello_Monitoring_AD_LDAP_to_SERVICES = {
        epg_source      = "SECINFRA_AD_LDAP"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["ICMP"])
      }

      Katello_Monitoring_SECU_REPO_to_SERVICES = {
        epg_source      = "SECINFRA_SECU_REPO"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["ICMP"])
      }

      Katello_Monitoring_EXCHANGE_to_SERVICES = {
        epg_source      = "SECINFRA_EXCHANGE"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["ICMP"])
      }

      Wazuh_Agents_GW_to_SECU_REPO = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SECU_REPO"
        ports           = jsonencode(["TCP_1514", "TCP_1515"])
      }

      Wazuh_Agents_SERVICES_to_SECU_REPO = {
        epg_source      = "SECINFRA_SERVICES"
        epg_destination = "SECINFRA_SECU_REPO"
        ports           = jsonencode(["TCP_1514", "TCP_1515"])
      }

      Wazuh_Agents_AD_LDAP_to_SECU_REPO = {
        epg_source      = "SECINFRA_AD_LDAP"
        epg_destination = "SECINFRA_SECU_REPO"
        ports           = jsonencode(["TCP_1514", "TCP_1515"])
      }

      Wazuh_Agents_EXCHANGE_to_SECU_REPO = {
        epg_source      = "SECINFRA_EXCHANGE"
        epg_destination = "SECINFRA_SECU_REPO"
        ports           = jsonencode(["TCP_1514", "TCP_1515"])
      }

      Wazuh_API_SECU_REPO_Internal = {
        epg_source      = "SECINFRA_SECU_REPO"
        epg_destination = "SECINFRA_SECU_REPO"
        ports           = jsonencode(["TCP_55000"])
      }

      Wazuh_Web_Admin_GW_to_SECU_REPO = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SECU_REPO"
        ports           = jsonencode(["TCP_443", "TCP_55000"])
      }

      Syslog_GW_to_SERVICES = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_514", "UDP_514", "TCP_5140", "TCP_6514"])
      }

      Syslog_AD_LDAP_to_SERVICES = {
        epg_source      = "SECINFRA_AD_LDAP"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_514", "UDP_514", "TCP_5140", "TCP_6514"])
      }

      Syslog_SECU_REPO_to_SERVICES = {
        epg_source      = "SECINFRA_SECU_REPO"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_514", "UDP_514", "TCP_5140", "TCP_6514"])
      }

      Syslog_EXCHANGE_to_SERVICES = {
        epg_source      = "SECINFRA_EXCHANGE"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_514", "UDP_514", "TCP_5140", "TCP_6514"])
      }

      CRL_Access_GW_to_SERVICES = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_80"])
      }

      CRL_Access_AD_LDAP_to_SERVICES = {
        epg_source      = "SECINFRA_AD_LDAP"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_80"])
      }

      CRL_Access_SECU_REPO_to_SERVICES = {
        epg_source      = "SECINFRA_SECU_REPO"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_80"])
      }

      CRL_Access_EXCHANGE_to_SERVICES = {
        epg_source      = "SECINFRA_EXCHANGE"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_80"])
      }

      WSUS_Clients_GW_to_SERVICES = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_8530", "TCP_443", "TCP_8531"])
      }

      WSUS_Clients_SECU_REPO_to_SERVICES = {
        epg_source      = "SECINFRA_SECU_REPO"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_8530", "TCP_443", "TCP_8531"])
      }

      KeePass_Access_GW_to_SERVICES = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_445"])
      }

      NTLite_Admin_GW_to_SERVICES = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_445", "TCP_80", "TCP_443"])
      }

      Proxy_Access_SERVICES_to_EXCHANGE = {
        epg_source      = "SECINFRA_SERVICES"
        epg_destination = "SECINFRA_EXCHANGE"
        ports           = jsonencode(["TCP_3128"])
      }

      Proxy_Access_GW_to_EXCHANGE = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_EXCHANGE"
        ports           = jsonencode(["TCP_3128"])
      }

      Proxy_Access_SECU_REPO_to_EXCHANGE = {
        epg_source      = "SECINFRA_SECU_REPO"
        epg_destination = "SECINFRA_EXCHANGE"
        ports           = jsonencode(["TCP_3128"])
      }

      LDAP_Sync_AD_LDAP_to_SERVICES = {
        epg_source      = "SECINFRA_AD_LDAP"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_389", "TCP_636"])
      }

      Admin_RDP_GW_to_AD_LDAP = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_AD_LDAP"
        ports           = jsonencode(["TCP_3389", "TCP_5985-5986"])
      }

      Admin_RDP_GW_to_SERVICES = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_3389", "TCP_5985-5986", "TCP_445"])
      }

      Admin_RDP_GW_to_SECU_REPO = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SECU_REPO"
        ports           = jsonencode(["TCP_3389", "TCP_5985-5986"])
      }

      Admin_SSH_GW_to_AD_LDAP = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_AD_LDAP"
        ports           = jsonencode(["TCP_22"])
      }

      Admin_SSH_GW_to_SERVICES = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_22"])
      }

      Admin_SSH_GW_to_SECU_REPO = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_SECU_REPO"
        ports           = jsonencode(["TCP_22"])
      }

      Admin_SSH_GW_to_EXCHANGE = {
        epg_source      = "SECINFRA_GW"
        epg_destination = "SECINFRA_EXCHANGE"
        ports           = jsonencode(["TCP_22"])
      }

      Services_SERVICES_to_GW = {
        epg_source      = "SECINFRA_SERVICES"
        epg_destination = "SECINFRA_GW"
        ports           = jsonencode(["TCP_53", "UDP_53", "UDP_88", "UDP_123"])
      }

      Services_SECU_REPO_to_GW = {
        epg_source      = "SECINFRA_SECU_REPO"
        epg_destination = "SECINFRA_GW"
        ports           = jsonencode(["TCP_53", "UDP_53", "UDP_88", "UDP_123"])
      }

      Services_EXCHANGE_to_GW = {
        epg_source      = "SECINFRA_EXCHANGE"
        epg_destination = "SECINFRA_GW"
        ports           = jsonencode(["TCP_53", "UDP_53", "UDP_88", "UDP_123", "TCP_80", "TCP_443"])
      }

      Services_Internal_WEF = {
        epg_source      = "SECINFRA_SERVICES"
        epg_destination = "SECINFRA_SERVICES"
        ports           = jsonencode(["TCP_5140"])
      }

      Services_SERVICES_to_SECU_REPO_WEF = {
        epg_source      = "SECINFRA_SERVICES"
        epg_destination = "SECINFRA_SECU_REPO"
        ports           = jsonencode(["TCP_5140"])
      }

      Services_EXCHANGE_to_SERVICES = {
        epg_source      = "SECINFRA_EXCHANGE"
        epg_destination = "SECINFRA_SERVICES"
        ports = jsonencode([
          "TCP_53", "TCP_88", "TCP_445", "TCP_5140", "TCP_8530",
          "UDP_53", "UDP_88", "UDP_123"
        ])
      }

      Services_SECU_REPO_to_SERVICES = {
        epg_source      = "SECINFRA_SECU_REPO"
        epg_destination = "SECINFRA_SERVICES"
        ports = jsonencode([
          "TCP_53", "TCP_88", "TCP_445", "TCP_5140", "TCP_8530",
          "UDP_53", "UDP_88", "UDP_123"
        ])
      }

      Services_EXCHANGE_to_SECU_REPO = {
        epg_source      = "SECINFRA_EXCHANGE"
        epg_destination = "SECINFRA_SECU_REPO"
        ports           = jsonencode(["TCP_53", "TCP_88", "UDP_53", "UDP_88", "UDP_123"])
      }
    }
  }
}

module "Contract" {
  source          = "git::https://gitlab.internal.example.com/infra/terraform/modules/iaasv2/iaasv2-contract.git?ref=1.0"
  for_each        = local.contracts[terraform.workspace]
  name            = each.key
  object_code     = var.object_code
  epg_source      = module.EPG[each.value.epg_source].output_epg_name
  epg_destination = module.EPG[each.value.epg_destination].output_epg_name
  ports           = each.value.ports
  depends_on      = [module.EPG]
}

locals {
  WEITContracts = {

    PROD_DC1 = {

    }

    PROD_DC2 = {

      # Monitoring PUSH (SECINFRA → MONITOR)
      SECINFRA_GW_TO_MONITOR = {
        src        = "SECINFRA_GW"
        dst_tenant = "MONITOR-GLOBAL"
        dst        = "MONITOR_ALL_DC2"
        ports      = jsonencode(["TCP_10051"])
      }

      SECINFRA_SERVICES_TO_MONITOR = {
        src        = "SECINFRA_SERVICES"
        dst_tenant = "MONITOR-GLOBAL"
        dst        = "MONITOR_ALL_DC2"
        ports      = jsonencode(["TCP_10051"])
      }

      SECINFRA_AD_LDAP_TO_MONITOR = {
        src        = "SECINFRA_AD_LDAP"
        dst_tenant = "MONITOR-GLOBAL"
        dst        = "MONITOR_ALL_DC2"
        ports      = jsonencode(["TCP_10051"])
      }

      SECINFRA_SECU_REPO_TO_MONITOR = {
        src        = "SECINFRA_SECU_REPO"
        dst_tenant = "MONITOR-GLOBAL"
        dst        = "MONITOR_ALL_DC2"
        ports      = jsonencode(["TCP_10051"])
      }

      SECINFRA_EXCHANGE_TO_MONITOR = {
        src        = "SECINFRA_EXCHANGE"
        dst_tenant = "MONITOR-GLOBAL"
        dst        = "MONITOR_ALL_DC2"
        ports      = jsonencode(["TCP_10051"])
      }

      # Monitoring PULL (MONITOR → SECINFRA)
      MONITOR_TO_SECINFRA_GW = {
        src_tenant = "MONITOR-GLOBAL"
        src        = "MONITOR_ALL_DC2"
        dst        = "SECINFRA_GW"
        ports      = jsonencode(["TCP_10050"])
      }

      MONITOR_TO_SECINFRA_SERVICES = {
        src_tenant = "MONITOR-GLOBAL"
        src        = "MONITOR_ALL_DC2"
        dst        = "SECINFRA_SERVICES"
        ports      = jsonencode(["TCP_10050"])
      }

      MONITOR_TO_SECINFRA_AD_LDAP = {
        src_tenant = "MONITOR-GLOBAL"
        src        = "MONITOR_ALL_DC2"
        dst        = "SECINFRA_AD_LDAP"
        ports      = jsonencode(["TCP_10050"])
      }

      MONITOR_TO_SECINFRA_SECU_REPO = {
        src_tenant = "MONITOR-GLOBAL"
        src        = "MONITOR_ALL_DC2"
        dst        = "SECINFRA_SECU_REPO"
        ports      = jsonencode(["TCP_10050"])
      }

      MONITOR_TO_SECINFRA_EXCHANGE = {
        src_tenant = "MONITOR-GLOBAL"
        src        = "MONITOR_ALL_DC2"
        dst        = "SECINFRA_EXCHANGE"
        ports      = jsonencode(["TCP_10050"])
      }

      CHOCOLATEY_TO_SECINFRA = {
        src        = "SECINFRA_GW"
        dst_tenant = "SHARED_SERVICES"
        dst        = "PROD_MIDDLE"
        ports      = jsonencode(["TCP_80"])
      }

      CHOCOLATEY_TO_SECINFRA_SERVICES = {
        src        = "SECINFRA_SERVICES"
        dst_tenant = "SHARED_SERVICES"
        dst        = "PROD_MIDDLE"
        ports      = jsonencode(["TCP_80"])
      }

      CHOCOLATEY_TO_SECINFRA_AD_LDAP = {
        src        = "SECINFRA_AD_LDAP"
        dst_tenant = "SHARED_SERVICES"
        dst        = "PROD_MIDDLE"
        ports      = jsonencode(["TCP_80"])
      }

      CHOCOLATEY_TO_SECINFRA_SECU_REPO = {
        src        = "SECINFRA_SECU_REPO"
        dst_tenant = "SHARED_SERVICES"
        dst        = "PROD_MIDDLE"
        ports      = jsonencode(["TCP_80"])
      }

      CHOCOLATEY_TO_SECINFRA_EXCHANGE = {
        src        = "SECINFRA_EXCHANGE"
        dst_tenant = "SHARED_SERVICES"
        dst        = "PROD_MIDDLE"
        ports      = jsonencode(["TCP_80"])
      }


    }
  }
}

module "WEITContract" {
  source      = "git::https://gitlab.internal.example.com/infra/terraform/modules/iaasv2/iaasv2-weintertenant.git?ref=1.0"
  for_each    = local.WEITContracts[terraform.workspace]
  name        = each.key
  object_code = var.object_code
  src_tenant  = try(each.value.src_tenant, var.object_code)
  src         = each.value.src
  dst_tenant  = try(each.value.dst_tenant, var.object_code)
  dst         = each.value.dst
  ports       = each.value.ports
  depends_on  = [module.EPG]
}
