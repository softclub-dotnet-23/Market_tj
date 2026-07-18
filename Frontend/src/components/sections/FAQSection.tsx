import { useState } from "react";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { Accordion, AccordionItem } from "@/components/ui/Accordion";
import { Chip } from "@/components/ui/Chip";
import { faqGroups, faqItems } from "@/data/faq";

export function FAQSection() {
  const [activeGroup, setActiveGroup] = useState(faqGroups[0]);
  const items = faqItems.filter((f) => f.group === activeGroup);

  return (
    <section className="bg-stone-50/60 py-14 sm:py-20 dark:bg-stone-900/40">
      <div className="container-page grid grid-cols-1 gap-12 lg:grid-cols-[0.8fr_1.2fr]">
        <div>
          <SectionHeading
            eyebrow="Вопросы"
            align="left"
            title="Часто задаваемые вопросы"
            description="Не нашли ответ? Напишите нам — раздел «Контакты» всегда на связи."
          />
          <div className="mt-8 flex flex-wrap gap-2">
            {faqGroups.map((group) => (
              <Chip key={group} active={group === activeGroup} onClick={() => setActiveGroup(group)}>
                {group}
              </Chip>
            ))}
          </div>
        </div>

        <div className="rounded-3xl border border-stone-100 bg-white px-6 sm:px-8 dark:border-stone-800 dark:bg-stone-900">
          <Accordion defaultOpen={String(items[0]?.id)}>
            {items.map((item) => (
              <AccordionItem key={item.id} id={String(item.id)} question={item.question}>
                {item.answer}
              </AccordionItem>
            ))}
          </Accordion>
        </div>
      </div>
    </section>
  );
}
